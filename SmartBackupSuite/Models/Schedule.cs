using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartBackupSuite.Models
{
    public class Schedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JobId { get; set; } = "";
        public string ScheduleType { get; set; } = "Daily"; // Once, Daily, Weekly, Monthly, Interval, Custom
        public DateTime StartTime { get; set; } = DateTime.Today.AddHours(2);
        public int IntervalMinutes { get; set; } = 60;
        public int DayOfWeek { get; set; } = 0; // 0=Saturday, 1=Sunday, ...
        public int DayOfMonth { get; set; } = 1;
        public DateTime? LastExecutionTime { get; set; }

        // ✅ للجدولة المخصصة
        public List<int> CustomDaysOfWeek { get; set; } = new List<int>(); // 0-6
        public List<string> CustomTimes { get; set; } = new List<string>(); // "08:00", "12:00"
        public int CustomExecutionCountPerDay { get; set; } = 1;

        // ✅ أوقات مختلفة لكل يوم (مفتاح = رقم اليوم)
        public Dictionary<int, List<string>> CustomDayTimes { get; set; } = new Dictionary<int, List<string>>();

        // ✅ خصائص التنفيذ الفائت
        public bool CatchUpMissed { get; set; } = true;
        public int MaxMissedExecutions { get; set; } = 5;
        public bool ExecuteOnStartup { get; set; } = true;

        /// <summary>
        /// الحصول على الأوقات المحددة ليوم معين
        /// </summary>
        public List<string> GetTimesForDay(int dayOfWeek)
        {
            if (CustomDayTimes != null && CustomDayTimes.ContainsKey(dayOfWeek))
                return CustomDayTimes[dayOfWeek];

            return CustomTimes;
        }

        /// <summary>
        /// تعيين أوقات ليوم معين
        /// </summary>
        public void SetTimesForDay(int dayOfWeek, List<string> times)
        {
            if (CustomDayTimes == null)
                CustomDayTimes = new Dictionary<int, List<string>>();

            CustomDayTimes[dayOfWeek] = times;
        }

        /// <summary>
        /// التحقق من وجود جدولة مخصصة
        /// </summary>
        public bool IsCustomSchedule => ScheduleType == "Custom" || ScheduleType == "مخصص";

        /// <summary>
        /// الحصول على وصف الجدولة للنص
        /// </summary>
        public string GetScheduleDescription()
        {
            string type = ScheduleType?.Trim() ?? "Daily";

            switch (type)
            {
                case "Once":
                case "مرة واحدة":
                    return $"مرة واحدة في {StartTime:HH:mm}";

                case "Daily":
                case "يومي":
                    return $"يومياً في {StartTime:HH:mm}";

                case "Weekly":
                case "أسبوعي":
                    string[] dayNames = { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" };
                    string dayName = (DayOfWeek >= 0 && DayOfWeek <= 6) ? dayNames[DayOfWeek] : "غير محدد";
                    return $"أسبوعياً في {dayName} الساعة {StartTime:HH:mm}";

                case "Monthly":
                case "شهري":
                    return $"شهرياً في يوم {DayOfMonth} الساعة {StartTime:HH:mm}";

                case "Interval":
                case "فترة زمنية":
                    return $"كل {IntervalMinutes} دقيقة (بدءاً من {StartTime:HH:mm})";

                case "Custom":
                case "مخصص":
                    if (CustomDaysOfWeek != null && CustomDaysOfWeek.Any())
                    {
                        string days = string.Join(", ", CustomDaysOfWeek.Select(d =>
                            new[] { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" }[d]));
                        string times = CustomTimes != null && CustomTimes.Any()
                            ? string.Join(", ", CustomTimes)
                            : "أوقات محددة";
                        return $"مخصص: {days} - {times}";
                    }
                    return "جدولة مخصصة";

                default:
                    return type;
            }
        }
    }
}