﻿using GP3.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using GP3.Client.Models;

namespace GP3.Client.ViewModels
{
    [QueryProperty("MonthReadings", "MonthReadings")]
    public partial class HistoryMonthAddViewModel : BaseViewModel
    {
        private readonly HistoryService _historyService;
        public HistoryMonthAddViewModel(HistoryService historyService)
        {
            _historyService = historyService;
            monthNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthGenitiveNames;
        }

        [ObservableProperty]
        string[] monthNames;

        [ObservableProperty]
        Month selectedMonth;

        [ObservableProperty]
        string kwhUsed;

        [ObservableProperty]
        string pricePayed;

        [ObservableProperty]
        ObservableCollection<MonthReading> monthReadings;

        [RelayCommand]
        async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task OnAdd()
        {
            /* TODO Add validation */
            MonthReading monthReadingItem = new MonthReading(selectedMonth, Double.Parse(pricePayed), int.Parse(kwhUsed));
            await _historyService.AddMonth(monthReadingItem);
            monthReadings.Add(monthReadingItem);
            await GoBackAsync();
        }
    }
}
