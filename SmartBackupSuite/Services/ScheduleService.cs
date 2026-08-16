using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SmartBackupSuite.Models;
using SmartBackupSuite.Helpers;

namespace SmartBackupSuite.Services
{
    public class ScheduleService : IDisposable
    {
        private System.Timers.Timer _timer;
        private bool _isRunning = false;
        private readonly object _lock = new object();
        private AppSettings _settings;
        private BackupService _backupService;

        public event EventHandler<string> JobExecuted;

        public ScheduleService()
        {
            _backupService = new BackupService();
            _timer = new System.Timers.Timer(60000); // التحقق كل دقيقة
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _timer.Start();
            LogService.WriteLog("ScheduleService", "Info", "✅ بدأت خدمة الجدولة");

            // تنفيذ فوري عند البدء مع التحقق من المهام الفائتة
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                CheckMissedJobsOnStartup();
                CheckAndExecuteJobs();
            });
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _timer.Stop();
            LogService.WriteLog("ScheduleService", "Info", "⏹️ توقفت خدمة الجدولة");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isRunning) return;
            CheckAndExecuteJobs();
        }

        public void CheckMissedJobsManually()
        {
            CheckMissedJobsOnStartup();
        }

        private void CheckMissedJobsOnStartup()
        {
            try
            {
                _settings = JsonFileHelper.ReadSettings();
                if (_settings == null || _settings.BackupJobs == null) return;

                var now = DateTime.Now;

                foreach (var job in _settings.BackupJobs.Where(j => j.IsActive && j.ExecuteMissedJobs))
                {
                    var schedule = _settings.Schedules.FirstOrDefault(s => s.JobId == job.Id);
                    if (schedule == null || !schedule.CatchUpMissed) continue;

                    CheckAndExecuteMissedJob(job, schedule, now);
                }

                LogService.WriteLog("ScheduleService", "Info", "✅ تم التحقق من المهام الفائتة عند بدء التشغيل");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ScheduleService", "Error", $"❌ خطأ في التحقق من المهام الفائتة: {ex.Message}");
            }
        }

        private void CheckAndExecuteMissedJob(BackupJob job, Schedule schedule, DateTime now)
        {
            try
            {
                if (job.LastRunTime == DateTime.MinValue) return;

                // حساب وقت التنفيذ التالي المتوقع
                DateTime? nextExpectedTime = GetNextExecutionTime(job, schedule, job.LastRunTime);
                if (nextExpectedTime.HasValue && nextExpectedTime.Value < now)
                {
                    var missedDuration = now - nextExpectedTime.Value;
                    var maxMissed = TimeSpan.FromHours(24);

                    if (missedDuration < maxMissed && job.MissedCount < schedule.MaxMissedExecutions)
                    {
                        // تنفيذ المهمة الفائتة
                        ExecuteJob(job);
                        job.MissedCount++;
                        job.LastRunTime = now;
                        UpdateJob(job);

                        LogService.WriteLog("ScheduleService", "Info",
                            $"✅ تم تنفيذ مهمة فائتة: {job.JobName} (فاتت بمقدار {missedDuration.TotalMinutes:F0} دقيقة)");
                    }
                    else if (missedDuration >= maxMissed || job.MissedCount >= schedule.MaxMissedExecutions)
                    {
                        LogService.WriteLog("ScheduleService", "Warning",
                            $"⚠️ تجاوزت المهمة {job.JobName} الحد الأقصى للفوات، تم تخطيها");
                        job.MissedCount = 0;
                        UpdateJob(job);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ScheduleService", "Error", $"❌ خطأ في تنفيذ المهمة الفائتة {job.JobName}: {ex.Message}");
            }
        }

        private DateTime? GetNextExecutionTime(BackupJob job, Schedule schedule, DateTime fromTime)
        {
            try
            {
                switch (schedule.ScheduleType?.ToLower())
                {
                    case "once":
                    case "مرة واحدة":
                        return schedule.StartTime > fromTime ? schedule.StartTime : (DateTime?)null;

                    case "daily":
                    case "يومي":
                        var nextDaily = fromTime.Date.Add(schedule.StartTime.TimeOfDay);
                        return nextDaily > fromTime ? nextDaily : nextDaily.AddDays(1);

                    case "weekly":
                    case "أسبوعي":
                        var daysAhead = (schedule.DayOfWeek - (int)fromTime.DayOfWeek + 7) % 7;
                        if (daysAhead == 0) daysAhead = 7;
                        var nextWeekly = fromTime.Date.AddDays(daysAhead).Add(schedule.StartTime.TimeOfDay);
                        return nextWeekly;

                    case "monthly":
                    case "شهري":
                        var nextMonthly = new DateTime(fromTime.Year, fromTime.Month, schedule.DayOfMonth);
                        if (nextMonthly <= fromTime)
                            nextMonthly = nextMonthly.AddMonths(1);
                        return nextMonthly.Add(schedule.StartTime.TimeOfDay);

                    case "interval":
                    case "فترة زمنية":
                        return fromTime.AddMinutes(schedule.IntervalMinutes);

                    case "custom":
                    case "مخصص":
                        return GetNextCustomExecutionTime(job, schedule, fromTime);

                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private DateTime? GetNextCustomExecutionTime(BackupJob job, Schedule schedule, DateTime fromTime)
        {
            try
            {
                int currentDay = (int)fromTime.DayOfWeek;
                var todayTimes = schedule.GetTimesForDay(currentDay);

                if (todayTimes != null && todayTimes.Any())
                {
                    var nextTime = todayTimes
                        .Select(t => fromTime.Date.Add(TimeSpan.Parse(t)))
                        .Where(t => t > fromTime)
                        .OrderBy(t => t)
                        .FirstOrDefault();

                    if (nextTime != DateTime.MinValue)
                        return nextTime;
                }

                // البحث في الأيام القادمة
                for (int i = 1; i <= 7; i++)
                {
                    int nextDay = (currentDay + i) % 7;
                    if (schedule.CustomDaysOfWeek != null && schedule.CustomDaysOfWeek.Contains(nextDay))
                    {
                        var dayTimes = schedule.GetTimesForDay(nextDay);
                        if (dayTimes != null && dayTimes.Any())
                        {
                            return fromTime.Date.AddDays(i).Add(TimeSpan.Parse(dayTimes.First()));
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void CheckAndExecuteJobs()
        {
            lock (_lock)
            {
                try
                {
                    _settings = JsonFileHelper.ReadSettings();
                    if (_settings == null || _settings.BackupJobs == null) return;

                    var now = DateTime.Now;

                    foreach (var job in _settings.BackupJobs.Where(j => j.IsActive))
                    {
                        var schedule = _settings.Schedules.FirstOrDefault(s => s.JobId == job.Id);
                        if (schedule == null) continue;

                        if (ShouldExecuteNow(job, schedule, now))
                        {
                            ExecuteJob(job);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.WriteLog("ScheduleService", "Error", $"❌ خطأ في التحقق من المهام: {ex.Message}");
                }
            }
        }

        private bool ShouldExecuteNow(BackupJob job, Schedule schedule, DateTime now)
        {
            try
            {
                if (job.LastRunTime != DateTime.MinValue)
                {
                    if ((now - job.LastRunTime).TotalMinutes < 1)
                        return false;
                }

                switch (schedule.ScheduleType?.ToLower())
                {
                    case "once":
                    case "مرة واحدة":
                        return schedule.StartTime.Date == now.Date &&
                               schedule.StartTime.TimeOfDay <= now.TimeOfDay &&
                               job.LastRunTime == DateTime.MinValue;

                    case "daily":
                    case "يومي":
                        return schedule.StartTime.TimeOfDay <= now.TimeOfDay &&
                               (job.LastRunTime == DateTime.MinValue ||
                                job.LastRunTime.Date < now.Date);

                    case "weekly":
                    case "أسبوعي":
                        int currentDay = (int)now.DayOfWeek;
                        return schedule.StartTime.TimeOfDay <= now.TimeOfDay &&
                               schedule.DayOfWeek == currentDay &&
                               (job.LastRunTime == DateTime.MinValue ||
                                job.LastRunTime.Date < now.Date);

                    case "monthly":
                    case "شهري":
                        return schedule.DayOfMonth == now.Day &&
                               schedule.StartTime.TimeOfDay <= now.TimeOfDay &&
                               (job.LastRunTime == DateTime.MinValue ||
                                job.LastRunTime.Date < now.Date);

                    case "interval":
                    case "فترة زمنية":
                        if (job.LastRunTime == DateTime.MinValue)
                            return schedule.StartTime.TimeOfDay <= now.TimeOfDay;

                        var elapsed = now - job.LastRunTime;
                        return elapsed.TotalMinutes >= schedule.IntervalMinutes;

                    case "custom":
                    case "مخصص":
                        return ShouldExecuteCustom(job, schedule, now);

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ScheduleService", "Error", $"❌ خطأ في التحقق من الجدولة: {ex.Message}");
                return false;
            }
        }

        private bool ShouldExecuteCustom(BackupJob job, Schedule schedule, DateTime now)
        {
            try
            {
                int currentDay = (int)now.DayOfWeek;

                if (schedule.CustomDaysOfWeek == null || !schedule.CustomDaysOfWeek.Contains(currentDay))
                    return false;

                var times = schedule.GetTimesForDay(currentDay);
                if (times == null || times.Count == 0)
                    return false;

                string currentTime = now.ToString("HH:mm");

                if (!times.Contains(currentTime))
                    return false;

                if (job.LastRunTime != DateTime.MinValue && job.LastRunTime.Date == now.Date)
                {
                    string lastRunTime = job.LastRunTime.ToString("HH:mm");
                    if (lastRunTime == currentTime)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ScheduleService", "Error", $"❌ خطأ في التحقق من الجدولة المخصصة: {ex.Message}");
                return false;
            }
        }

        private async void ExecuteJob(BackupJob job)
        {
            try
            {
                LogService.WriteLog("ScheduleService", "Info", $"▶️ تنفيذ المهمة: {job.JobName}");

                var connection = _settings.ServerConnections.FirstOrDefault(c => c.Id == job.ConnectionId);
                var destination = _settings.Destinations.FirstOrDefault(d => d.JobId == job.Id);

                if (connection == null || destination == null)
                {
                    job.LastRunStatus = "Failed - Connection/Destination not found";
                    job.LastRunTime = DateTime.Now;
                    UpdateJob(job);
                    LogService.WriteLog("ScheduleService", "Error", $"❌ فشل تنفيذ {job.JobName}: اتصال أو وجهة غير موجودة");
                    return;
                }

                bool success = await _backupService.ExecuteBackup(job, connection, destination);

                job.LastRunTime = DateTime.Now;
                job.LastRunStatus = success ? "Success" : "Failed";
                job.MissedCount = 0; // إعادة تعيين عداد الفوات بعد التنفيذ الناجح
                UpdateJob(job);

                string message = success ? $"✅ نجح التنفيذ: {job.JobName}" : $"❌ فشل التنفيذ: {job.JobName}";
                LogService.WriteLog("ScheduleService", success ? "Success" : "Error", message);

                OnJobExecuted(message);
            }
            catch (Exception ex)
            {
                job.LastRunTime = DateTime.Now;
                job.LastRunStatus = $"Failed - {ex.Message}";
                UpdateJob(job);
                LogService.WriteLog("ScheduleService", "Error", $"❌ خطأ في تنفيذ {job.JobName}: {ex.Message}");
                OnJobExecuted($"❌ فشل تنفيذ {job.JobName}: {ex.Message}");
            }
        }

        private void UpdateJob(BackupJob job)
        {
            try
            {
                var settings = JsonFileHelper.ReadSettings();
                var index = settings.BackupJobs.FindIndex(j => j.Id == job.Id);
                if (index >= 0)
                {
                    settings.BackupJobs[index] = job;
                    JsonFileHelper.SaveSettings(settings);
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ScheduleService", "Error", $"❌ خطأ في تحديث المهمة: {ex.Message}");
            }
        }

        protected virtual void OnJobExecuted(string message)
        {
            JobExecuted?.Invoke(this, message);
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
        }
    }
}