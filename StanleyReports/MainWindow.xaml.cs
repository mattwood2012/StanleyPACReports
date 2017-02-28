using Abt.Controls.SciChart.Model.DataSeries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StanleyReports
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Entry> entries;
        DateTime dataStartDate, dataEndDate;
        bool isLoaded = false;

        IOrderedEnumerable<IGrouping<int, Entry>> queryUsers;

        XyDataSeries<int,int> distributionDS = new XyDataSeries<int,int>();
        XyDataSeries<TimeSpan, int> entriesByDayDS = new XyDataSeries<TimeSpan,int>();

        public MainWindow()
        {
            InitializeComponent();

            entries = GetEvents();
            dataStartDate = entries.First().dateTime.Date;
            dataEndDate = entries.Last().dateTime.Date;

            queryUsers =
                from entry in entries
                group entry by entry.KeyholderID into userGroup
                orderby userGroup.Count()
                select userGroup;

            InitializeGUI(dataStartDate, dataEndDate);

            DisplayStatistics();
            //UpdateAllDisplays(); // entries);
        }

        private void InitializeGUI(DateTime dataStartDate, DateTime dataEndDate)
        {
            // Global
            startDate.DisplayDateStart = dataStartDate;
            startDate.DisplayDateEnd = dataEndDate;
            startDate.SelectedDate = dataStartDate;
            endDate.DisplayDateStart = dataStartDate;
            endDate.DisplayDateEnd = dataEndDate;
            endDate.SelectedDate = dataEndDate;

            // Entry Distribution
            distributionRenderableSeries.DataSeries = distributionDS;

            // Entries by day
            entriesByDayRenderableSeries.DataSeries = entriesByDayDS;
        }

        private List<Entry> GetEvents()
        {
            var result = new List<Entry>();
            var lines = File.ReadAllLines("Events.csv");

            string[] values;
            Entry e;
            DateTime minDate = new DateTime(2016, 1, 1);
            foreach (var line in lines)
            {
                values = line.Split(',');
                e = new Entry(DateTime.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]), int.Parse(values[3]));
                if (e.dateTime > minDate) result.Add(e);
            }

            return result;
        }

        private void UpdateAllDisplays() //List<Entry> entries)
        {
            isLoaded = true;    //Allow updates on GUI changes now

            DisplayStatistics(); //entries);
            DisplayEntryDistribution(null, null); // (entries);
        }

        #region Display statistics
        private void DisplayStatistics() //List<Entry> entries)
        {
            txtDataStartsOn.Text = String.Format("{0:MM/dd/yyyy} at {0:HH:mm:ss}", entries.First().dateTime);
            txtDataEndsOn.Text = String.Format("{0:MM/dd/yyyy} at {0:HH:mm:ss}", entries.Last().dateTime);
            txtTotalEntries.Text = entries.Count.ToString();
            txtUniqueEntries.Text = queryUsers.Count().ToString();

            #region Total Entries per day, week and month
            int aMin, aMax, aSum;

            Dictionary<DateTime, int> aEntriesPerDay = new Dictionary<DateTime, int>();
            var aStart = entries.First().dateTime.Date;
            var aEnd = entries.Last().dateTime.Date;
            int aDayCount = 0;

            while (aStart.AddDays(aDayCount) <= aEnd)
            {
                aEntriesPerDay.Add(aStart.AddDays(aDayCount++), 0);
            }

            foreach (var entry in entries)
            {
                aEntriesPerDay[entry.dateTime.Date]++;
            }

            TrimTotalDictionary(aEntriesPerDay, (bool)chkExcludeFirst.IsChecked, (bool)chkExcludeLast.IsChecked);

            aMin = int.MaxValue;
            aMax = int.MinValue;
            aSum = 0;
            int aValue;

            foreach (var key in aEntriesPerDay.Keys)
            {
                aValue = aEntriesPerDay[key];
                aMin = (aMin > aValue) ? aValue : aMin;
                aMax = (aMax < aValue) ? aValue : aMax;
                aSum += aValue;
            }
            txtTotalPerDayMin.Text = aMin.ToString();
            txtTotalPerDayMax.Text = aMax.ToString();
            txtTotalPerDaySum.Text = (aSum / aEntriesPerDay.Count).ToString();
            txtTotalDayPeriods.Text = aEntriesPerDay.Count.ToString();

            Dictionary<DateTime, int> aEntriesPerWeek = new Dictionary<DateTime, int>();
            aStart = entries.First().dateTime.Date.AddDays(-(int)entries.First().dateTime.DayOfWeek);
            aEnd = entries.Last().dateTime.Date.AddDays(-(int)entries.Last().dateTime.DayOfWeek);
            aDayCount = 0;

            while (aStart.AddDays(aDayCount) <= aEnd)
            {
                aEntriesPerWeek.Add(aStart.AddDays(aDayCount), 0);
                aDayCount += 7;
            }


            foreach (var entry in entries)
            {
                aEntriesPerWeek[entry.dateTime.Date.AddDays(-(int)entry.dateTime.DayOfWeek)]++;
            }

            TrimTotalDictionary(aEntriesPerWeek, (bool)chkExcludeFirst.IsChecked, (bool)chkExcludeLast.IsChecked);

            aMin = int.MaxValue;
            aMax = int.MinValue;
            aSum = 0;

            foreach (var key in aEntriesPerWeek.Keys)
            {
                aValue = aEntriesPerWeek[key];
                aMin = (aMin > aValue) ? aValue : aMin;
                aMax = (aMax < aValue) ? aValue : aMax;
                aSum += aValue;
            }
            txtTotalPerWeekMin.Text = aMin.ToString();
            txtTotalPerWeekMax.Text = aMax.ToString();
            txtTotalPerWeekSum.Text = (aSum / aEntriesPerWeek.Count).ToString();
            txtTotalWeekPeriods.Text = aEntriesPerWeek.Count.ToString();

            Dictionary<DateTime, int> aEntriesPerMonth = new Dictionary<DateTime, int>();
            aStart = new DateTime(entries.First().dateTime.Year, entries.First().dateTime.Month, 1);
            aEnd = new DateTime(entries.Last().dateTime.Year, entries.Last().dateTime.Month, 1);
            int aMonthCount = 0;

            while (aStart.AddMonths(aMonthCount) <= aEnd)
            {
                aEntriesPerMonth.Add(aStart.AddMonths(aMonthCount++), 0);
            }

            foreach (var entry in entries)
            {
                aEntriesPerMonth[new DateTime(entry.dateTime.Year, entry.dateTime.Month, 1)]++;
            }

            TrimTotalDictionary(aEntriesPerMonth, (bool)chkExcludeFirst.IsChecked, (bool)chkExcludeLast.IsChecked);

            aMin = int.MaxValue;
            aMax = int.MinValue;
            aSum = 0;

            foreach (var key in aEntriesPerMonth.Keys)
            {
                aValue = aEntriesPerMonth[key];
                aMin = (aMin > aValue) ? aValue : aMin;
                aMax = (aMax < aValue) ? aValue : aMax;
                aSum += aValue;
            }
            txtTotalPerMonthMin.Text = aMin.ToString();
            txtTotalPerMonthMax.Text = aMax.ToString();
            txtTotalPerMonthSum.Text = (aSum / aEntriesPerMonth.Count).ToString();
            txtTotalMonthPeriods.Text = aEntriesPerMonth.Count.ToString();

            #endregion

            #region Unique Entries per day, week and month
            int min, max, sum;

            Dictionary<DateTime, List<int>> entriesPerDay = new Dictionary<DateTime, List<int>>();
            var start = entries.First().dateTime.Date;
            var end = entries.Last().dateTime.Date;
            int dayCount = 0;

            while (start.AddDays(dayCount) <= end)
            {
                entriesPerDay.Add(start.AddDays(dayCount++), new List<int>());
            }

            foreach (var entry in entries)
            {
                if (!entriesPerDay[entry.dateTime.Date].Contains(entry.KeyholderID))
                {
                    entriesPerDay[entry.dateTime.Date].Add(entry.KeyholderID);
                }
            }

            TrimDictionary(entriesPerDay, (bool)chkExcludeFirst.IsChecked, (bool)chkExcludeLast.IsChecked);

            min = int.MaxValue;
            max = int.MinValue;
            sum = 0;
            int value;

            foreach (var key in entriesPerDay.Keys)
            {
                value = entriesPerDay[key].Count;
                min = (min > value) ? value : min;
                max = (max < value) ? value : max;
                sum += value;
            }
            txtUniquePerDayMin.Text = min.ToString();
            txtUniquePerDayMax.Text = max.ToString();
            txtUniquePerDaySum.Text = (sum / entriesPerDay.Count).ToString();
            txtUniqueDayPeriods.Text = entriesPerDay.Count.ToString();

            Dictionary<DateTime, List<int>> entriesPerWeek = new Dictionary<DateTime, List<int>>();
            DateTime weekKey;
            start = entries.First().dateTime.Date.AddDays(-(int)entries.First().dateTime.DayOfWeek);
            end = entries.Last().dateTime.Date.AddDays(-(int)entries.Last().dateTime.DayOfWeek);
            dayCount = 0;

            while (start.AddDays(dayCount) <= end)
            {
                entriesPerWeek.Add(start.AddDays(dayCount), new List<int>());
                dayCount += 7;
            }

            foreach (var entry in entries)
            {
                weekKey = entry.dateTime.Date.AddDays(-(int)entry.dateTime.DayOfWeek);

                if (!entriesPerWeek[weekKey].Contains(entry.KeyholderID))
                {
                    entriesPerWeek[weekKey].Add(entry.KeyholderID);
                }
            }

            TrimDictionary(entriesPerWeek, (bool)chkExcludeFirst.IsChecked, (bool)chkExcludeLast.IsChecked);

            min = int.MaxValue;
            max = int.MinValue;
            sum = 0;

            foreach (var key in entriesPerWeek.Keys)
            {
                value = entriesPerWeek[key].Count;
                min = (min > value) ? value : min;
                max = (max < value) ? value : max;
                sum += value;
            }
            txtUniquePerWeekMin.Text = min.ToString();
            txtUniquePerWeekMax.Text = max.ToString();
            txtUniquePerWeekSum.Text = (sum / entriesPerWeek.Count).ToString();
            txtUniqueWeekPeriods.Text = entriesPerWeek.Count.ToString();

            Dictionary<DateTime, List<int>> entriesPerMonth = new Dictionary<DateTime, List<int>>();
            DateTime monthKey;
            start = new DateTime(entries.First().dateTime.Year, entries.First().dateTime.Month, 1);
            end = new DateTime(entries.Last().dateTime.Year, entries.Last().dateTime.Month, 1);
            int monthCount = 0;

            while (start.AddMonths(monthCount) <= end)
            {
                entriesPerMonth.Add(start.AddMonths(monthCount++), new List<int>());
            }


            foreach (var entry in entries)
            {

                monthKey = new DateTime(entry.dateTime.Year, entry.dateTime.Month, 1);

                if (!entriesPerMonth[monthKey].Contains(entry.KeyholderID))
                {
                    entriesPerMonth[monthKey].Add(entry.KeyholderID);
                }
            }

            TrimDictionary(entriesPerMonth, (bool)chkExcludeFirst.IsChecked, (bool)chkExcludeLast.IsChecked);

            min = int.MaxValue;
            max = int.MinValue;
            sum = 0;

            foreach (var key in entriesPerMonth.Keys)
            {
                value = entriesPerMonth[key].Count;
                min = (min > value) ? value : min;
                max = (max < value) ? value : max;
                sum += value;
            }
            txtUniquePerMonthMin.Text = min.ToString();
            txtUniquePerMonthMax.Text = max.ToString();
            txtUniquePerMonthSum.Text = (sum / entriesPerMonth.Count).ToString();
            txtUniqueMonthPeriods.Text = entriesPerMonth.Count.ToString();

            #endregion

        }
        #endregion

        #region Entry distribution
        private void DisplayEntryDistribution(object sender, RoutedEventArgs e) //List<Entry> entries)
        {

        Dictionary<int, int> freqDist = new Dictionary<int, int>();

            foreach (var userGroup in queryUsers)
            {
                if (!freqDist.ContainsKey(userGroup.Count()))
                {
                    freqDist.Add(userGroup.Count(), 1);
                }
                else
                {
                    freqDist[userGroup.Count()]++;
                }
            }

            int[] numberOfusers = new int[freqDist.Last().Key + 1];

            foreach (var fd in freqDist)
            {
                numberOfusers[fd.Key] = fd.Value;
            }

            distributionDS.Clear();
            var xs = Enumerable.Range(0, numberOfusers.Count() - 1);
            for (int i = 0; i < numberOfusers.Count(); i++)
            {
                distributionDS.Append(i, numberOfusers[i]);
            }
            //distributionDS.Append(xs, numberOfusers);
        }
        #endregion

        #region Entries per day
        private void DisplayEntriesByDay(object sender, RoutedEventArgs e) //List<Entry> entries)
        {
            TimeSpan window = new TimeSpan(0, 60, 0);
            var windowTicks = window.Ticks;

            var groupedInDay =
                            (from entry in entries
                            let percentile = GetTimeSpan(entry, windowTicks)
                            group new { entry.KeyholderID } by percentile into timeSpanGroup
                            orderby timeSpanGroup.Key
                            select new { Name = timeSpanGroup.Key, Data = timeSpanGroup.Count() })
                            .ToDictionary(z => z.Name, z => z.Data); // { Name = timeSpanGroup.Key, Data = timeSpanGroup.Count() }; //timeSpanGroup; // new KeyValuePair<TimeSpan,int>(timeSpanGroup.Key, timeSpanGroup.Count<>());

            // Create a dictionary that has all time windows as keys and count or zero as value
            Dictionary < TimeSpan, int> entryGroups = new Dictionary<TimeSpan, int>();
            Int64 keyAsTicks = 0;
            TimeSpan key;
            do
            {
                key = new TimeSpan(keyAsTicks);
                if (groupedInDay.ContainsKey(key))
                {
                    entryGroups.Add(key, groupedInDay[key]);
                }
                else
                {
                    entryGroups.Add(key, 0);
                }

                keyAsTicks += windowTicks;
            } while (keyAsTicks < TimeSpan.TicksPerDay);

            entriesByDayDS.Clear();
            entriesByDayDS.Append(entryGroups.Keys, entryGroups.Values);
        }

        private static TimeSpan GetTimeSpan(Entry entry, Int64 windowTicks)
        {
            var entryTicks = entry.dateTime.TimeOfDay.Ticks;
            var whole = entryTicks / windowTicks;
            var ts = new TimeSpan(whole * windowTicks);
            return new TimeSpan(whole * windowTicks);
        }
        #endregion

        #region Helper methods
        private void TrimTotalDictionary(Dictionary<DateTime, int> dictionary, bool excludeFirst, bool excludeLast)
        {
            if (excludeLast && (dictionary.Count > 0))
                dictionary.Remove(dictionary.Keys.Last());
            if (excludeFirst && (dictionary.Count > 0))
                dictionary.Remove(dictionary.Keys.First());
        }

        private void TrimDictionary(Dictionary<DateTime, List<int>> dictionary, bool excludeFirst, bool excludeLast)
        {
            if (excludeLast && (dictionary.Count > 0))
                dictionary.Remove(dictionary.Keys.Last());
            if (excludeFirst && (dictionary.Count > 0))
                dictionary.Remove(dictionary.Keys.First());
        }
        #endregion
    }
}
