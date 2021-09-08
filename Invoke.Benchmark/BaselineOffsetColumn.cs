#nullable enable

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Invoke.Benchmark
{
    public class BaselineOffsetColumn : BaselineCustomColumn
    {
        public string? ScaleToParams { get; }

        public BaselineOffsetColumn(string scaleToParams) => ScaleToParams = scaleToParams;
        public override string Id => nameof(BaselineOffsetColumn) + (ScaleToParams == null ? "" : ScaleToParams);

        public override string ColumnName => "Diff Ratio" + (ScaleToParams == null ? "" : "( scaled to " + ScaleToParams + ")");

        public override int PriorityInCategory => 3;

        public override bool IsNumeric => true;

        public override UnitType UnitType => UnitType.Dimensionless;

        public override string Legend => "Mean of the ratio distribution ([Current]-[Baseline])/[Baseline]";

        private static Statistics? GetRatioStatistics(Statistics? current, Statistics? scaleTo)
        {
            if (current == null || current.N < 1)
                return null;
            if (scaleTo == null || scaleTo.N < 1)
                return null;
            try
            {
                return Statistics.Divide(current, scaleTo);
            }
            catch (DivideByZeroException)
            {
                return null;
            }
        }
        private static Statistics Subtract(Statistics x, Statistics y)
        {
            if (x.N < 1)
                throw new ArgumentOutOfRangeException(nameof(x), "Argument doesn't contain any values");
            if (y.N < 1)
                throw new ArgumentOutOfRangeException(nameof(y), "Argument doesn't contain any values");
            var z = new double[(Math.Min(x.N, y.N))];
            for (int i = 0; i < z.Length; i++)
                z[i] = x.OriginalValues[i] - y.OriginalValues[i];

            return new Statistics(z);
        }
        public override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, IReadOnlyDictionary<string, Metric> baselineMetrics, Statistics current, IReadOnlyDictionary<string, Metric> currentMetrics, bool isBaseline)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var scaleToCase = summary.BenchmarksCases.FirstOrDefault(b => summary.GetLogicalGroupKey(b) == logicalGroupKey && b.Parameters.ValueInfo == ScaleToParams);
            var scaleTo = scaleToCase == null
                ? baseline
                : Subtract(summary[scaleToCase].ResultStatistics, baseline);

            Statistics offset = Subtract(current, baseline);
            var ratio = GetRatioStatistics(offset, scaleTo);
            if (ratio == null)
                return "NA";
            var invertedRatio = GetRatioStatistics(scaleTo, offset);

            var cultureInfo = summary.GetCultureInfo();
            var ratioStyle = summary.Style.RatioStyle;

            bool advancedPrecision = IsNonBaselinesPrecise(summary, scaleTo, benchmarkCase);
            switch (ratioStyle)
            {
                case RatioStyle.Value:
                    return ratio.Mean.ToString(advancedPrecision ? "N3" : "N2", cultureInfo);
                case RatioStyle.Percentage:
                    return isBaseline
                        ? "baseline"
                        : ratio.Mean >= 1.0
                            ? "+" + ((ratio.Mean - 1.0) * 100).ToString(advancedPrecision ? "N1" : "N0", cultureInfo) + "%"
                            : "-" + ((1.0 - ratio.Mean) * 100).ToString(advancedPrecision ? "N1" : "N0", cultureInfo) + "%";
                case RatioStyle.Trend:
                    return isBaseline
                        ? "baseline"
                        : ratio.Mean >= 1.0
                            ? ratio.Mean.ToString(advancedPrecision ? "N3" : "N2", cultureInfo) + "x slower"
                            : invertedRatio == null
                                ? "NA"
                                : invertedRatio.Mean.ToString(advancedPrecision ? "N3" : "N2", cultureInfo) + "x faster";
                default:
                    throw new ArgumentOutOfRangeException(nameof(summary), ratioStyle, "RatioStyle is not supported");
            }
        }
        private static bool IsNonBaselinesPrecise(Summary summary, Statistics baselineStat, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var nonBaselines = summary.GetNonBaselines(logicalGroupKey);
            return nonBaselines.Any(x => GetRatioStatistics(summary[x].ResultStatistics, baselineStat)?.Mean < 0.01);
        }
    }
}
