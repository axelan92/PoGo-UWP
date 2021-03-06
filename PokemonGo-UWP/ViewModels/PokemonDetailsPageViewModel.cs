﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using PokemonGo_UWP.Entities;
using PokemonGo_UWP.Utils;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Universal_Authenticator_v2.Views;

namespace PokemonGo_UWP.ViewModels
{
    public class PokemonDetailsPageViewModel : ViewModelBase
    {

        #region Lifecycle Handlers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter">MapPokemonWrapper containing the Pokemon that we're trying to capture</param>
        /// <param name="mode"></param>
        /// <param name="suspensionState"></param>
        /// <returns></returns>
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode,
            IDictionary<string, object> suspensionState)
        {
            if (suspensionState.Any())
            {
                // Recovering the state
                CurrentPokemon = (PokemonDataWrapper) suspensionState[nameof(CurrentPokemon)];
                PlayerProfile = (PlayerData) suspensionState[nameof(PlayerProfile)];
            }
            else
            {
                // Navigating from inventory page so we need to load the pokemon               
                CurrentPokemon = (PokemonDataWrapper) NavigationHelper.NavigationState[nameof(CurrentPokemon)];
                UpdateCurrentData();
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Save state before navigating
        /// </summary>
        /// <param name="suspensionState"></param>
        /// <param name="suspending"></param>
        /// <returns></returns>
        public override async Task OnNavigatedFromAsync(IDictionary<string, object> suspensionState, bool suspending)
        {
            if (suspending)
            {
                suspensionState[nameof(CurrentPokemon)] = CurrentPokemon;
                suspensionState[nameof(PlayerProfile)] = PlayerProfile;
            }
            await Task.CompletedTask;
        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }

        #endregion

        #region Game Management Vars

        /// <summary>
        ///     Pokemon that we're trying to capture
        /// </summary>
        private PokemonDataWrapper _currentPokemon;

        /// <summary>
        /// Data for the current user
        /// </summary>
        private PlayerData _playerProfile;

        /// <summary>
        /// Pokedex data for the current Pokemon
        /// </summary>
        private PokemonSettings _pokemonExtraData;

        /// <summary>
        /// Amount of Stardust owned by the player
        /// </summary>
        private int _stardustAmount;

        /// <summary>
        /// Candies needed to powerup the Pokemon
        /// </summary>
        private int _candiesToPowerUp;

        /// <summary>
        /// Stardust needed to evolve the Pokemon
        /// </summary>
        private int _stardustToPowerUp;

        /// <summary>
        /// Candy type for the current Pokemon
        /// </summary>
        private Candy _currentCandy;

        /// <summary>
        /// Result of Pokemon evolution
        /// </summary>
        private EvolvePokemonResponse _evolvePokemonResponse;

        #endregion

        #region Bindable Game Vars

        /// <summary>
        ///     Pokemon that we're trying to capture
        /// </summary>
        public PokemonDataWrapper CurrentPokemon
        {
            get { return _currentPokemon; }
            set
            {
                Set(ref _currentPokemon, value);
                RaisePropertyChanged(() => EvolvedPokemonId);
            }
        }

        /// <summary>
        /// Id for current pokemon's evolution
        /// </summary>
        public PokemonId EvolvedPokemonId => CurrentPokemon?.PokemonId + 1 ?? 0;

        /// <summary>
        /// Data for the current user
        /// </summary>
        public PlayerData PlayerProfile
        {
            get { return _playerProfile; }
            set { Set(ref _playerProfile, value); }
        }

        /// <summary>
        /// Pokedex data for the current Pokemon
        /// </summary>
        public PokemonSettings PokemonExtraData
        {
            get { return _pokemonExtraData; }
            set { Set(ref _pokemonExtraData, value); }
        }

        /// <summary>
        /// Amount of Stardust owned by the player
        /// </summary>
        public int StardustAmount
        {
            get { return _stardustAmount; }
            set { Set(ref _stardustAmount, value); }
        }

        /// <summary>
        /// Candies needed to powerup the Pokemon
        /// </summary>
        public int CandiesToPowerUp
        {
            get { return _candiesToPowerUp; }
            set
            {
                Set(ref _candiesToPowerUp, value);
                PowerUpPokemonCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Stardust needed to evolve the Pokemon
        /// </summary>
        public int StardustToPowerUp
        {
            get { return _stardustToPowerUp; }
            set
            {
                Set(ref _stardustToPowerUp, value);
                PowerUpPokemonCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Candy type for the current Pokemon
        /// </summary>
        public Candy CurrentCandy
        {
            get { return _currentCandy; }
            set
            {
                Set(ref _currentCandy, value); 
                EvolvePokemonCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Result of Pokemon evolution
        /// </summary>
        public EvolvePokemonResponse EvolvePokemonResponse
        {
            get { return _evolvePokemonResponse; }
            set
            {
                Set(ref _evolvePokemonResponse, value);
                EvolvePokemonCommand.RaiseCanExecuteChanged();
            }
        }


        #endregion

        #region Game Logic

        #region Shared Logic

        /// <summary>
        /// Updates data related to current Pokemon
        /// </summary>
        private void UpdateCurrentData()
        {
            // Retrieve data
            PlayerProfile = GameClient.PlayerProfile;
            StardustAmount = PlayerProfile.Currencies.FirstOrDefault(item => item.Name.Equals("STARDUST")).Amount;
            var upgradeCosts =
                GameClient.PokemonUpgradeCosts[
                    Convert.ToInt32(Math.Round(PokemonInfo.GetLevel(CurrentPokemon.WrappedData)) - 1)];
            CandiesToPowerUp = Convert.ToInt32(upgradeCosts[0]);
            StardustToPowerUp = Convert.ToInt32(upgradeCosts[1]);
            PokemonExtraData = GameClient.PokedexExtraData.FirstOrDefault(item => item.PokemonId == CurrentPokemon.PokemonId);            
            CurrentCandy = GameClient.CandyInventory.FirstOrDefault(item => item.FamilyId == PokemonExtraData.FamilyId);
            RaisePropertyChanged(() => PokemonExtraData);
        }

        private DelegateCommand _returnToPokemonInventoryScreen;

        /// <summary>
        ///     Going back to inventory page
        /// </summary>
        public DelegateCommand ReturnToPokemonInventoryScreen => _returnToPokemonInventoryScreen ?? (
            _returnToPokemonInventoryScreen = new DelegateCommand(() =>
            {
                NavigationService.GoBack();
            }, () => true)
            );

        #endregion

        #region Transfer

        private DelegateCommand _transferPokemonCommand;

        public DelegateCommand TransferPokemonCommand => _transferPokemonCommand ?? (
          _transferPokemonCommand = new DelegateCommand(() =>
          {
              //var pk = CurrentPokemon;
              //var mes = await GameClient.TransferPokemon(pk.Id);
              //MessageDialog mes1 = new MessageDialog(mes.Result.ToString());
              //await mes1.ShowAsync();
              //await GameClient.UpdateInventory();
              //NavigationHelper.NavigationState["CurrentPokemon"] = CurrentPokemon;
              //BootStrapper.Current.NavigationService.Navigate(typeof(PokemonView), true);
          }, () => true));

        #endregion

        #region Power Up

        private DelegateCommand _powerUpPokemonCommand;

        public DelegateCommand PowerUpPokemonCommand => _powerUpPokemonCommand ?? (
          _powerUpPokemonCommand = new DelegateCommand(async () =>
          {
              // Send power up request
              var res = await GameClient.PowerUpPokemon(CurrentPokemon.WrappedData);              
              switch (res.Result)
              {
                  case UpgradePokemonResponse.Types.Result.Unset:
                      break;
                  case UpgradePokemonResponse.Types.Result.Success:
                      // Reload updated data
                      CurrentPokemon = new PokemonDataWrapper(res.UpgradedPokemon);
                      await GameClient.UpdateInventory();
                      await GameClient.UpdateProfile();
                      UpdateCurrentData();
                      break;
                  // TODO: do something if we have an error!
                  case UpgradePokemonResponse.Types.Result.ErrorPokemonNotFound:
                      break;
                  case UpgradePokemonResponse.Types.Result.ErrorInsufficientResources:
                      break;
                  case UpgradePokemonResponse.Types.Result.ErrorUpgradeNotAvailable:
                      break;
                  case UpgradePokemonResponse.Types.Result.ErrorPokemonIsDeployed:
                      break;
                  default:
                      throw new ArgumentOutOfRangeException();
              }
          }, () => StardustAmount >= StardustToPowerUp));

        #endregion

        #region Evolve

        #region Evolve Events

        public event EventHandler PokemonEvolved;

        #endregion

        private DelegateCommand _evolvePokemonCommand;

        public DelegateCommand EvolvePokemonCommand => _evolvePokemonCommand ?? (_evolvePokemonCommand = new DelegateCommand(async () =>
        {                                 
            EvolvePokemonResponse = await GameClient.EvolvePokemon(CurrentPokemon.WrappedData);            
            switch (EvolvePokemonResponse.Result)
            {
                case EvolvePokemonResponse.Types.Result.Unset:
                    break;
                case EvolvePokemonResponse.Types.Result.Success:
                    PokemonEvolved?.Invoke(this, null);                    
                    await GameClient.UpdateInventory();
                    await GameClient.UpdateProfile();                 
                    break;
                // TODO: do something if we have an error!
                case EvolvePokemonResponse.Types.Result.FailedPokemonMissing:
                    break;
                case EvolvePokemonResponse.Types.Result.FailedInsufficientResources:
                    break;
                case EvolvePokemonResponse.Types.Result.FailedPokemonCannotEvolve:
                    break;
                case EvolvePokemonResponse.Types.Result.FailedPokemonIsDeployed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }, () => CurrentCandy != null && CurrentCandy.Candy_ >= PokemonExtraData.CandyToEvolve));

        private DelegateCommand _replaceEvolvedPokemonCommand;

        public DelegateCommand ReplaceEvolvedPokemonCommand => _replaceEvolvedPokemonCommand ?? (_replaceEvolvedPokemonCommand = new DelegateCommand(
            () =>
        {
            CurrentPokemon = new PokemonDataWrapper(EvolvePokemonResponse.EvolvedPokemonData);
            UpdateCurrentData();
        }));

        #endregion

        #endregion
    }
}
