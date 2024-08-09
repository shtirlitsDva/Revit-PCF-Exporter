using System;
using System.Collections.Generic;
using System.Text;

namespace MEPUtils.CreateInstrumentation
{
    public enum Schedule
    {
        None,
        C02,
        C03,
        C08,
        C08_10_50,
        C08_65_500,
        S02,
        S03,
    }
    public interface ISchedule
    {
        string Name { get; }
        Schedule Schedule { get; }
        string PipeTypeNormal { get; }
        string PipeTypeTap { get; }
    }
    public abstract class NN_Schedule : ISchedule
    {
        public string Name { get => this.Schedule.ToString(); }
        public abstract Schedule Schedule { get; }
        public abstract string PipeTypeNormal { get; }
        public abstract string PipeTypeTap { get; }
        public static Dictionary<Schedule, ISchedule> Schedules =>
            new Dictionary<Schedule, ISchedule>
            {
                { Schedule.C02, new NN_C02() },
                { Schedule.C03, new NN_C03() },
                { Schedule.C08, new NN_C08() },
                { Schedule.C08_10_50, new NN_C08_10_50() },
                { Schedule.C08_65_500, new NN_C08_65_500() },
                { Schedule.S02, new NN_S02() },
                { Schedule.S03, new NN_S03() },
            };
    }
    public class NN_C02 : NN_Schedule
    {
        public override Schedule Schedule => Schedule.C02;
        public override string PipeTypeNormal => "Pipe_Class_C02_15-500_EN10220_SMLS_WLD";
        public override string PipeTypeTap => "Pipe_Class_C02_15-500_EN10220_SMLS_WLD_TAP";
    }
    public class NN_C03 : NN_Schedule
    {
        public override Schedule Schedule => Schedule.C03;
        public override string PipeTypeNormal => "Pipe_Class_C03_15-500_EN10220_SMLS_WLD";
        public override string PipeTypeTap => "Pipe_Class_C03_15-500_EN10220_SMLS_WLD_TAP";
    }
    public class NN_C08_10_50 : NN_Schedule
    {
        public override Schedule Schedule => Schedule.C08_10_50;
        public override string PipeTypeNormal => "Pipe_Class_C08_10-50_EN10255_SMLS_THRD";
        public override string PipeTypeTap => "Pipe_Class_C08_10-50_EN10255_SMLS_THRD_TAP";
    }
    public class NN_C08 : NN_Schedule
    {
        public override Schedule Schedule => Schedule.C08;
        public override string PipeTypeNormal => "Pipe_Class_C08_10-500_EN10220_WLD_WLD";
        public override string PipeTypeTap => "Pipe_Class_C08_10-500_EN10220_WLD_WLD_TAP";
    }
    public class NN_C08_65_500 : NN_Schedule
    {
        public override Schedule Schedule => Schedule.C08_65_500;
        public override string PipeTypeNormal => "Pipe_Class_C08_65-500_EN10220_WLD_WLD";
        public override string PipeTypeTap => "Pipe_Class_C08_65-500_EN10220_WLD_WLD_TAP";
    }
    public class NN_S02 : NN_Schedule
    {
        public override Schedule Schedule => Schedule.S02;
        public override string PipeTypeNormal => "Pipe_Class_S02_8-600_ISO1127_WLD_WLD";
        public override string PipeTypeTap => "Pipe_Class_S02_8-600_ISO1127_WLD_WLD_TAP";
    }
    public class NN_S03 : NN_Schedule
    {
        public override Schedule Schedule => Schedule.S03;
        public override string PipeTypeNormal => "Pipe_Class_S03_8-350_ISO1127_WLD_WLD";
        public override string PipeTypeTap => "Pipe_Class_S03_8-350_ISO1127_WLD_WLD_TAP";
    }
}
