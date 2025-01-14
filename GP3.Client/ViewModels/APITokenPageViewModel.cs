﻿
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GP3.Client.Models;
using GP3.Client.Refit;
using GP3.Common.Entities;
using System.Collections.ObjectModel;

namespace GP3.Client.ViewModels;

[QueryProperty("MeterHistoryCollection", "MeterHistoryCollection")]
[QueryProperty("Provider", "Provider")]
public partial class APITokenPageViewModel : BaseViewModel
{
    private readonly IHistoryApi _api;
    private Color redColor = new Color(226, 54, 54);
    private Color greyColor = new Color(246, 246, 246);

    public APITokenPageViewModel(IHistoryApi api)
    {
        tokenFieldBorderColor = greyColor;
        _api = api;
        errorText = "";
    }

    [ObservableProperty]
    public ProviderSelection provider;

    [ObservableProperty]
    string apiToken;

    [ObservableProperty]
    Color tokenFieldBorderColor;

    [ObservableProperty]
    public ObservableCollection<MeterHistory> meterHistoryCollection;

    [ObservableProperty]
    public string errorText;

    [RelayCommand]
    void ApiTokenClicked()
    {
        TokenFieldBorderColor = greyColor;
        ErrorText = "";
    }

    [RelayCommand]
    async void AddMeter()
    {
        if(IsBusy)
            return;

        if(string.IsNullOrEmpty(ApiToken))
        {
            ActivateError("Please fill the field!");
            return;
        }

        IsBusy = true;
        try
        {
            await _api.RegisterProvider(new HistoryRegistration("Nedas", ApiToken, Provider));
            await Shell.Current.GoToAsync("../..");
        }
        catch(Exception e)
        {
            await Shell.Current.DisplayAlert("Error!", e.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }

    }
    void ActivateError(string errorText)
    {
        ErrorText = errorText;
        TokenFieldBorderColor = redColor;
    }
   
}

