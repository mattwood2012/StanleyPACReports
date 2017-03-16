﻿using Abt.Controls.SciChart.Model.DataSeries;
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
        List<Entry> allEntries;
        List<Entry> globallyFilteredEntries;
        string[] doors;
        DateTime dataStartDate, dataEndDate;
        bool isLoaded = false;

        //IOrderedEnumerable<IGrouping<int, Entry>> queryUsers;

        XyDataSeries<DateTime, int> entriesAllDS = new XyDataSeries<DateTime, int>();
        XyDataSeries<TimeSpan, int> entriesByDayDS = new XyDataSeries<TimeSpan,int>();
        XyDataSeries<DateTime, int> entriesByWeekDS = new XyDataSeries<DateTime, int>();
        XyDataSeries<DateTime, int> entriesByMonthDS = new XyDataSeries<DateTime, int>();
        XyDataSeries<int,int> distributionDS = new XyDataSeries<int,int>();

        public MainWindow()
        {
            InitializeComponent();

            allEntries = GetEvents();
            doors = File.ReadAllLines("Doors.csv");

            dataStartDate = allEntries.First().dateTime.Date;
            dataEndDate = allEntries.Last().dateTime.Date;

            InitializeGUI(dataStartDate, dataEndDate);

            globallyFilteredEntries = ApplyGlobalFilters(allEntries);

            isLoaded = true;
            DisplayStatistics(null, null);
        }

        #region Display statistics
        private void DisplayStatistics(object sender, RoutedEventArgs e)
        {
            StatisticsGrid.Visibility = (globallyFilteredEntries.Count == 0) ? Visibility.Collapsed : Visibility.Visible;
            StatisticsMissingDataText.Visibility = (globallyFilteredEntries.Count != 0) ? Visibility.Collapsed : Visibility.Visible;

            if (globallyFilteredEntries.Count == 0) return;

            txtDataStartsOn.Text = String.Format("{0:MM/dd/yyyy} at {0:HH:mm:ss}", globallyFilteredEntries.First().dateTime);
            txtDataEndsOn.Text = String.Format("{0:MM/dd/yyyy} at {0:HH:mm:ss}", globallyFilteredEntries.Last().dateTime);
            txtTotalEntries.Text = globallyFilteredEntries.Count.ToString();
            txtUniqueEntries.Text =  (from e1 in globallyFilteredEntries select e1.KeyholderID).Distinct().Count().ToString();

            #region Total Entries per day, week and month

            // Totals per day
            Dictionary<DateTime, int> aEntriesPerDay = new Dictionary<DateTime, int>();
            var aStart = globallyFilteredEntries.First().dateTime.Date;
            var aEnd = globallyFilteredEntries.Last().dateTime.Date;
            int aDayCount = 0;

            while (aStart.AddDays(aDayCount) <= aEnd)
            {
                aEntriesPerDay.Add(aStart.AddDays(aDayCount++), 0);
            }

            foreach (var entry in globallyFilteredEntries)
            {
                aEntriesPerDay[entry.dateTime.Date]++;
            }

            DisplayStatsTotal(aEntriesPerDay, txtTotalPerDayMin, txtTotalPerDayMax, txtTotalPerDaySum, txtTotalDayPeriods);

            // Totals per week
            Dictionary<DateTime, int> aEntriesPerWeek = new Dictionary<DateTime, int>();
            aStart = globallyFilteredEntries.First().dateTime.Date.AddDays(-(int)globallyFilteredEntries.First().dateTime.DayOfWeek);
            aEnd = globallyFilteredEntries.Last().dateTime.Date.AddDays(-(int)globallyFilteredEntries.Last().dateTime.DayOfWeek);
            aDayCount = 0;

            while (aStart.AddDays(aDayCount) <= aEnd)
            {
                aEntriesPerWeek.Add(aStart.AddDays(aDayCount), 0);
                aDayCount += 7;
            }

            foreach (var entry in globallyFilteredEntries)
            {
                aEntriesPerWeek[entry.dateTime.Date.AddDays(-(int)entry.dateTime.DayOfWeek)]++;
            }

            DisplayStatsTotal(aEntriesPerWeek, txtTotalPerWeekMin, txtTotalPerWeekMax, txtTotalPerWeekSum, txtTotalWeekPeriods);

            // Totals per month
            Dictionary<DateTime, int> aEntriesPerMonth = new Dictionary<DateTime, int>();
            aStart = new DateTime(globallyFilteredEntries.First().dateTime.Year, globallyFilteredEntries.First().dateTime.Month, 1);
            aEnd = new DateTime(globallyFilteredEntries.Last().dateTime.Year, globallyFilteredEntries.Last().dateTime.Month, 1);
            int aMonthCount = 0;

            while (aStart.AddMonths(aMonthCount) <= aEnd)
            {
                aEntriesPerMonth.Add(aStart.AddMonths(aMonthCount++), 0);
            }

            foreach (var entry in globallyFilteredEntries)
            {
                aEntriesPerMonth[new DateTime(entry.dateTime.Year, entry.dateTime.Month, 1)]++;
            }

            DisplayStatsTotal(aEntriesPerMonth,txtTotalPerMonthMin, txtTotalPerMonthMax, txtTotalPerMonthSum, txtTotalMonthPeriods);

            #endregion

            #region Unique Entries per day, week and month

            // Uniques per day
            Dictionary<DateTime, List<int>> entriesPerDay = new Dictionary<DateTime, List<int>>();
            var start = globallyFilteredEntries.First().dateTime.Date;
            var end = globallyFilteredEntries.Last().dateTime.Date;
            int dayCount = 0;

            while (start.AddDays(dayCount) <= end)
            {
                entriesPerDay.Add(start.AddDays(dayCount++), new List<int>());
            }

            foreach (var entry in globallyFilteredEntries)
            {
                if (!entriesPerDay[entry.dateTime.Date].Contains(entry.KeyholderID))
                {
                    entriesPerDay[entry.dateTime.Date].Add(entry.KeyholderID);
                }
            }

            DisplayStatsUnique(entriesPerDay, txtUniquePerDayMin, txtUniquePerDayMax, txtUniquePerDaySum, txtUniqueDayPeriods);

            // Uniques per week
            Dictionary<DateTime, List<int>> entriesPerWeek = new Dictionary<DateTime, List<int>>();
            DateTime weekKey;
            start = globallyFilteredEntries.First().dateTime.Date.AddDays(-(int)globallyFilteredEntries.First().dateTime.DayOfWeek);
            end = globallyFilteredEntries.Last().dateTime.Date.AddDays(-(int)globallyFilteredEntries.Last().dateTime.DayOfWeek);
            dayCount = 0;

            while (start.AddDays(dayCount) <= end)
            {
                entriesPerWeek.Add(start.AddDays(dayCount), new List<int>());
                dayCount += 7;
            }

            foreach (var entry in globallyFilteredEntries)
            {
                weekKey = entry.dateTime.Date.AddDays(-(int)entry.dateTime.DayOfWeek);

                if (!entriesPerWeek[weekKey].Contains(entry.KeyholderID))
                {
                    entriesPerWeek[weekKey].Add(entry.KeyholderID);
                }
            }

            DisplayStatsUnique(entriesPerWeek, txtUniquePerWeekMin, txtUniquePerWeekMax, txtUniquePerWeekSum, txtUniqueWeekPeriods);

            // Uniques per month
            Dictionary<DateTime, List<int>> entriesPerMonth = new Dictionary<DateTime, List<int>>();
            DateTime monthKey;
            start = new DateTime(globallyFilteredEntries.First().dateTime.Year, globallyFilteredEntries.First().dateTime.Month, 1);
            end = new DateTime(globallyFilteredEntries.Last().dateTime.Year, globallyFilteredEntries.Last().dateTime.Month, 1);
            int monthCount = 0;

            while (start.AddMonths(monthCount) <= end)
            {
                entriesPerMonth.Add(start.AddMonths(monthCount++), new List<int>());
            }

            foreach (var entry in globallyFilteredEntries)
            {

                monthKey = new DateTime(entry.dateTime.Year, entry.dateTime.Month, 1);

                if (!entriesPerMonth[monthKey].Contains(entry.KeyholderID))
                {
                    entriesPerMonth[monthKey].Add(entry.KeyholderID);
                }
            }

            DisplayStatsUnique(entriesPerMonth, txtUniquePerMonthMin, txtUniquePerMonthMax, txtUniquePerMonthSum, txtUniqueMonthPeriods);

            #endregion

        }

        private void DisplayStatsTotal(Dictionary<DateTime, int> entriesPer, TextBlock txtMin, TextBlock txtMax, TextBlock txtSum, TextBlock txtPeriods)
        {
            int aMin, aMax, aSum, aValue;
            TrimTotalDictionary(entriesPer, (bool)chkStatsExcludeFirst.IsChecked, (bool)chkStatsExcludeLast.IsChecked);

            if (entriesPer.Count() == 0)
            {
                aMin = aMax = aSum = 0;
            }
            else
            {
                aMin = int.MaxValue;
                aMax = int.MinValue;
                aSum = 0;

                foreach (var key in entriesPer.Keys)
                {
                    aValue = entriesPer[key];
                    aMin = (aMin > aValue) ? aValue : aMin;
                    aMax = (aMax < aValue) ? aValue : aMax;
                    aSum += aValue;
                }
            }

            txtMin.Text = aMin.ToString();
            txtMax.Text = aMax.ToString();
            txtSum.Text = (entriesPer.Count > 0) ? (aSum / entriesPer.Count).ToString() : "0";
            txtPeriods.Text = entriesPer.Count.ToString();
        }

        private void DisplayStatsUnique(Dictionary<DateTime, List<int>> entriesPer, TextBlock txtMin, TextBlock txtMax, TextBlock txtSum, TextBlock txtPeriods)
        {
            int aMin, aMax, aSum, aValue;
            TrimDictionary(entriesPer, (bool)chkStatsExcludeFirst.IsChecked, (bool)chkStatsExcludeLast.IsChecked);

            if (entriesPer.Count() == 0)
            {
                aMin = aMax = aSum = 0;
            }
            else
            {
                aMin = int.MaxValue;
                aMax = int.MinValue;
                aSum = 0;

                foreach (var key in entriesPer.Keys)
                {
                    aValue = entriesPer[key].Count();
                    aMin = (aMin > aValue) ? aValue : aMin;
                    aMax = (aMax < aValue) ? aValue : aMax;
                    aSum += aValue;
                }
            }

            txtMin.Text = aMin.ToString();
            txtMax.Text = aMax.ToString();
            txtSum.Text = (entriesPer.Count > 0) ? (aSum / entriesPer.Count).ToString() : "0";
            txtPeriods.Text = entriesPer.Count.ToString();
        }
        #endregion

        #region All Entries
        private void DisplayEntriesAll(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            if (globallyFilteredEntries.Count == 0)
            {
                entriesAllDS.Clear();
                return;
            }


            Dictionary<DateTime, List<int>> allEntriesDictionary = new Dictionary<DateTime, List<int>>();
            //Dictionary<DateTime, int> allEntriesDictionary = new Dictionary<DateTime, int>();

            long windowTicks = GetBinTimeSpan(entriesAllWindow).Ticks;

            DateTime startingDate = (DateTime)startDate.SelectedDate;
            long endingTicks = ((DateTime)endDate.SelectedDate).Date.Ticks + TimeSpan.TicksPerDay;
            int entryPtr = 0;
            DateTime key;
            long keyTicks = globallyFilteredEntries.First().dateTime.Date.Ticks;

            // Calculate a count for each day of week in total date range
            do
            {

                key = new DateTime(keyTicks);
                allEntriesDictionary.Add(key, new List<int>());
                //allEntriesDictionary.Add(key, 0);

                while (globallyFilteredEntries[entryPtr++].dateTime.Ticks < (keyTicks + windowTicks))
                {
                    allEntriesDictionary[key].Add(globallyFilteredEntries[entryPtr++].KeyholderID);
                    //allEntriesDictionary[key]++;
                    if (entryPtr == globallyFilteredEntries.Count)
                        break;
                }
                keyTicks += windowTicks;

            } while ((keyTicks <= endingTicks) && (entryPtr < globallyFilteredEntries.Count));

            entriesAllDS.Clear();
            if ((bool)chkAllEntriesUnique.IsChecked)
            {
                foreach (var periodEntries in allEntriesDictionary)
                {
                    entriesAllDS.Append(periodEntries.Key, periodEntries.Value.Distinct().Count());
                }
            }
            else
            {
                foreach (var periodEntries in allEntriesDictionary)
                {
                    entriesAllDS.Append(periodEntries.Key, periodEntries.Value.Count());
                }
            }
            //var keys = allEntriesDictionary.Keys.ToList();
            //var values = allEntriesDictionary.Values.ToList();
            //entriesAllDS.Append(allEntriesDictionary.Keys, allEntriesDictionary.Values);
        }

        #endregion

        #region Entries per day
        private void DisplayEntriesByDay(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            if (globallyFilteredEntries.Count == 0)
            {
                entriesByDayDS.Clear();
                return;
            }

            //TimeSpan window = GetBinTimeSpan(entriesByDayWindow);
            Dictionary<DateTime, Dictionary<TimeSpan, List<int>>> allDayGroups = new Dictionary<DateTime, Dictionary<TimeSpan, List<int>>>();

            long windowTicks = GetBinTimeSpan(entriesByDayWindow).Ticks;
            DateTime dateKey;

            // Then add the ID for each entry falling in a bin
            foreach (var entry in globallyFilteredEntries)
            {
                dateKey = entry.dateTime.Date;
                if (!allDayGroups.ContainsKey(dateKey))
                {
                    allDayGroups.Add(dateKey, CreateDayGroup(windowTicks));
                }
                allDayGroups[dateKey][GetTimeSpanForDay(entry, windowTicks)].Add(entry.KeyholderID);
            }

            // Sum up all days
            bool unique = (bool)chkEntriesByDayUnique.IsChecked;
            Dictionary<TimeSpan, int> groupedInDay = new Dictionary<TimeSpan, int>();
            int i = 0;
            while (i * windowTicks < TimeSpan.TicksPerDay)
            {
                groupedInDay.Add(new TimeSpan(i++ * windowTicks), 0);
            } 

            foreach (var dayGroupKey in allDayGroups.Keys)
            {
                foreach (var binGroup in allDayGroups[dayGroupKey])
                {
                    if (unique)
                    {
                        groupedInDay[binGroup.Key] += binGroup.Value.Distinct().Count();
                    }
                    else
                    {
                        groupedInDay[binGroup.Key] += binGroup.Value.Count();
                    }
                }
            }

            // Set chart x/y values
            int scaleFactor;

            if ((bool)entriesByDayShowAverage.IsChecked)
            {
                scaleFactor = (globallyFilteredEntries.Last().dateTime.Date - globallyFilteredEntries.First().dateTime.Date).Days + 1;
            }
            else
            {
                scaleFactor = 1;
            }

            entriesByDayDS.Clear();

            foreach (var grouping in groupedInDay)
            {
                entriesByDayDS.Append(grouping.Key, grouping.Value / scaleFactor);
            }
        }

        private Dictionary<TimeSpan, List<int>> CreateDayGroup(long windowTicks)
        {
            Dictionary<TimeSpan, List<int>> result = new Dictionary<TimeSpan, List<int>>();
            
            // Add an entry for each time period (a.k.a bin a.k.a. window)
            int i = 0;
            while (i * windowTicks < TimeSpan.TicksPerDay)
            {
                result.Add(new TimeSpan(i++ * windowTicks), new List<int>());
            }

            return result;
        }

        private TimeSpan GetTimeSpanForDay(Entry entry, Int64 windowTicks)
        {
            var entryTicks = entry.dateTime.TimeOfDay.Ticks;
            long bin = entryTicks / windowTicks;
            return new TimeSpan(bin * windowTicks);
        }

        #endregion

        #region Entries per week
        private void DisplayEntriesByWeek(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            if (globallyFilteredEntries.Count == 0)
            {
                entriesByWeekDS.Clear();
                return;
            }

            TimeSpan window = GetBinTimeSpan(entriesByWeekWindow);

            var groupedInWeek =
                            (from entry in globallyFilteredEntries
                             let groupKey = GetTimeSpanForWeek(entry, window.Ticks)
                             group new { entry.KeyholderID } by groupKey into timeSpanGroup
                             orderby timeSpanGroup.Key
                             select new { Name = timeSpanGroup.Key, Data = timeSpanGroup.Count() })
                            .ToDictionary(z => z.Name, z => z.Data);

            // Create a dictionary that has all time windows as keys and count or zero as value
            Dictionary<DateTime, int> entryGroups = new Dictionary<DateTime, int>();
            Int64 keyAsTicks = 0;
            TimeSpan groupedInWeekKey;
            DateTime key;
            bool showAverage = (bool)entriesByWeekShowAverage.IsChecked;
            int[] dayOfWeekCount = new int[7];
            int count = 0;
            int value;
            DateTime startingDate = (DateTime)startDate.SelectedDate;
            DateTime endingDate = (DateTime)endDate.SelectedDate;

            // Correct for DateTime.MinValue being a Sunday so when TimeSpan key is converted back to date the correct day of week is displayed
            long dayOfWeekOffset = 6 * TimeSpan.TicksPerDay; 

            // Calculate a count for each day of week in total date range
            do
            {
                dayOfWeekCount[(int)((startingDate.AddDays(count)).DayOfWeek)]++;
            } while (startingDate.AddDays(++count) <= endingDate.Date);

            if (showAverage)
                for (int i = 0; i < 7; i++) System.Diagnostics.Debug.WriteLine((DayOfWeek)i + " " + dayOfWeekCount[i]);
            else
                System.Diagnostics.Debug.WriteLine("Showing Totals");

            do
            {
                groupedInWeekKey = new TimeSpan(keyAsTicks);
                key = new DateTime(groupedInWeekKey.Ticks + dayOfWeekOffset);
                if (groupedInWeek.ContainsKey(groupedInWeekKey))
                {
                    if (showAverage)
                    {
                        value = (dayOfWeekCount[(int)key.DayOfWeek] == 0) ? 0 : groupedInWeek[groupedInWeekKey] / dayOfWeekCount[(int)key.DayOfWeek];
                        entryGroups.Add(key, value);
                    }
                    else
                        entryGroups.Add(key, groupedInWeek[groupedInWeekKey]);
                }
                else
                {
                    entryGroups.Add(key, 0);
                }

                keyAsTicks += window.Ticks;
            } while (keyAsTicks < TimeSpan.TicksPerDay * 7);

            entriesByWeekDS.Clear();
            entriesByWeekDS.Append(entryGroups.Keys, entryGroups.Values);
        }

        private static TimeSpan GetTimeSpanForWeek(Entry entry, Int64 windowTicks)
        {
            var entryTicks = ((int)entry.dateTime.DayOfWeek) * TimeSpan.TicksPerDay + entry.dateTime.TimeOfDay.Ticks;
            long bin = entryTicks / windowTicks;
            return new TimeSpan(bin * windowTicks);
        }

        #endregion

        #region Entries per month
        private void DisplayEntriesByMonth(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            if (globallyFilteredEntries.Count == 0)
            {
                entriesByMonthDS.Clear();
                return;
            }

            TimeSpan window = GetBinTimeSpan(entriesByMonthWindow);

            var groupedInMonth =
                            (from entry in globallyFilteredEntries
                             let percentile = GetTimeSpanForMonth(entry, window.Ticks)
                             group new { entry.KeyholderID } by percentile into timeSpanGroup
                             orderby timeSpanGroup.Key
                             select new { Name = timeSpanGroup.Key, Data = timeSpanGroup.Count() })
                            .ToDictionary(z => z.Name, z => z.Data);

            // Create a dictionary that has all time windows as keys and count or zero as value
            Dictionary<DateTime, int> entryGroups = new Dictionary<DateTime, int>();
            Int64 keyAsTicks = 0;
            TimeSpan groupedInMonthKey;
            DateTime key;
            bool showAverage = (bool)entriesByMonthShowAverage.IsChecked;
            int[] dayOfMonthCount = new int[32];
            int count = 0;
            int value;
            //TimeSpan oneDay = new TimeSpan(1, 0, 0, 0);

            // Calculate a count for each day of month in total date range
            DateTime startingDate = (DateTime)startDate.SelectedDate;
            DateTime endingDate = (DateTime)endDate.SelectedDate;
            do
            {
                dayOfMonthCount[(int)((startingDate.AddDays(count)).Day)]++;
            } while (startingDate.AddDays(++count) <= endingDate.Date);

            if (showAverage)
                for (int i = 1; i < 32; i++) System.Diagnostics.Debug.WriteLine(i + " " + dayOfMonthCount[i]);
            else
                System.Diagnostics.Debug.WriteLine("Showing Totals");

            do
            {
                groupedInMonthKey = new TimeSpan(keyAsTicks + TimeSpan.TicksPerDay);
                key = new DateTime(keyAsTicks);
                if (groupedInMonth.ContainsKey(groupedInMonthKey))
                {
                    if (showAverage)
                    {
                        value = (dayOfMonthCount[(int)key.Day] == 0) ? 0 : groupedInMonth[groupedInMonthKey] / dayOfMonthCount[(int)key.Day];
                        entryGroups.Add(key, value);
                    }
                    else
                        entryGroups.Add(key, groupedInMonth[groupedInMonthKey]);
                }
                else
                {
                    entryGroups.Add(key, 0);
                }

                keyAsTicks += window.Ticks;
            } while (keyAsTicks < TimeSpan.TicksPerDay * 32);

            entriesByMonthDS.Clear();
            entriesByMonthDS.Append(entryGroups.Keys, entryGroups.Values);
        }

        private static TimeSpan GetTimeSpanForMonth(Entry entry, Int64 windowTicks)
        {
            var entryTicks = ((int)entry.dateTime.Day) * TimeSpan.TicksPerDay + entry.dateTime.TimeOfDay.Ticks;
            long bin = entryTicks / windowTicks;
            var ts = new TimeSpan(bin * windowTicks);
            return new TimeSpan(bin * windowTicks);
        }

        #endregion

        #region Entry distribution
        private void DisplayEntryDistribution(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            if (globallyFilteredEntries.Count == 0)
            {
                distributionDS.Clear();
                return;
            }


            Dictionary<int, int> freqDist = new Dictionary<int, int>();

            var groupedUsers =
                from entry in globallyFilteredEntries
                group entry by entry.KeyholderID into userGroup
                orderby userGroup.Count()
                select userGroup;

            foreach (var userGroup in groupedUsers)
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
        }
        #endregion


        #region Private helper methods

        private List<Entry> ApplyGlobalFilters(List<Entry> allEntries)
        {
            List<Entry> results;

            DateTime start = ((DateTime)startDate.SelectedDate).Date;
            DateTime end = ((DateTime)endDate.SelectedDate).Date;

            if (IsLoaded && (cboDoor.SelectedIndex != 0))
            {
                string idString = cboDoor.SelectedItem.ToString().Split('-')[1];
                int iD = int.Parse(idString);
                results = (from e in allEntries
                           where (e.dateTime.Date >= start) && (e.dateTime.Date <= end) && (e.EntrySourceID == iD)
                           select e).ToList<Entry>();
            }
            else
            {
                results = (from e in allEntries
                           where (e.dateTime.Date >= start) && (e.dateTime.Date <= end)
                           select e).ToList<Entry>();
            }

            return results;
        }

        private void InitializeGUI(DateTime dataStartDate, DateTime dataEndDate)
        {
            DataContext = this;

            // Global
            startDate.DisplayDateStart = dataStartDate;
            startDate.DisplayDateEnd = dataEndDate;
            startDate.SelectedDate = dataStartDate;
            endDate.DisplayDateStart = dataStartDate;
            endDate.DisplayDateEnd = dataEndDate;
            endDate.SelectedDate = dataEndDate;

            cboDoor.Items.Add(new ComboBoxItem() {Content="All", IsSelected = true });
            //ComboBoxItem cbi;
            foreach (string door in doors)
            {
                cboDoor.Items.Add(door.Split(',')[0] + " - " + door.Split(',')[1]);
            }

            // Fill theme combo box and set default theme
            foreach (string theme in Abt.Controls.SciChart.ThemeManager.AllThemes)
            {
                cboTheme.Items.Add(theme);
            }
            cboTheme.SelectedItem = "Chrome";

            // All Entries
            entriesAllRenderableSeries.DataSeries = entriesAllDS;

            // Entries by day
            entriesByDayRenderableSeries.DataSeries = entriesByDayDS;

            // Entries by week
            entriesByWeekRenderableSeries.DataSeries = entriesByWeekDS;

            // Entries by month
            entriesByMonthRenderableSeries.DataSeries = entriesByMonthDS;

            // Entry Distribution
            distributionRenderableSeries.DataSeries = distributionDS;
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

            DisplayStatistics(null, null); //entries);
            DisplayEntryDistribution(null, null); // (entries);
        }

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

        private TimeSpan GetBinTimeSpan (ComboBox cbo)
        {
            int windowMinutes;

            var textWindow = cbo.SelectedValue.ToString().Split(' ');
            if (textWindow[2] == "Minutes")
            {
                windowMinutes = int.Parse(textWindow[1]);
            }
            else
            {
                windowMinutes = int.Parse(textWindow[1]) * 60;
            }

            return new TimeSpan(0, windowMinutes, 0);
        }
        #endregion

        #region Code behind - I know, make it binding - will do once features have been defined

        private void GlobalSettingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            dataStartDate = (DateTime)startDate.SelectedDate;
            dataEndDate = (DateTime)endDate.SelectedDate;

            globallyFilteredEntries = ApplyGlobalFilters(allEntries);

            switch (ReportsTabControl.SelectedIndex)
            {
                case 0:
                    DisplayStatistics(null, null);
                    break;
                case 1:
                    DisplayEntriesAll(null, null);
                    break;
                case 2:
                    DisplayEntriesByDay(null, null);
                    break;
                case 3:
                    DisplayEntriesByWeek(null, null);
                    break;
                case 4:
                    DisplayEntriesByMonth(null, null);
                    break;
                case 5:
                    DisplayEntryDistribution(null, null);
                    break;
                default:
                    break;
            }
        }

        //private void XMajorLines_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (IsLoaded) EntryDistribYAxis.DrawMajorGridLines = true;
        //}

        //private void XMajorLines_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (IsLoaded) EntryDistribYAxis.DrawMajorGridLines = false;
        //}

        //private void entriesByDayWindow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{

        //}

        #endregion
    }
}
