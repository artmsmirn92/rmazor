﻿using DI.Extensions;
using DialogViewers;
using Entities;
using Games.RazorMaze.Models;
using Games.RazorMaze.Views.InputConfigurators;
using Games.RazorMaze.Views.MazeItems;
using Ticker;
using Utils;

namespace Games.RazorMaze.Views.UI
{
    public class ViewUI : ViewUIBase
    {
        #region inject

        private IUITicker                   UITicker             { get; }
        private IBigDialogViewer            BigDialogViewer      { get; }
        private IProposalDialogViewer       ProposalDialogViewer { get; }
        private IDialogPanels               DialogPanels         { get; }
        private IViewInputCommandsProceeder CommandsProceeder    { get; }
        private IManagersGetter             Managers             { get; }

        public ViewUI(
            IUITicker                   _UITicker,
            IBigDialogViewer            _BigDialogViewer,
            IProposalDialogViewer       _ProposalDialogViewer,
            IDialogPanels               _DialogPanels,
            IViewUIGameControls         _GameControls,
            IViewInputCommandsProceeder _CommandsProceeder,
            IManagersGetter             _Managers)
            : base(_GameControls)
        {
            UITicker = _UITicker;
            BigDialogViewer = _BigDialogViewer;
            ProposalDialogViewer = _ProposalDialogViewer;
            DialogPanels = _DialogPanels;
            CommandsProceeder = _CommandsProceeder;
            Managers = _Managers;
        }

        #endregion
        
        #region api

        public override void Init()
        {
            UITicker.Register(this);
            CommandsProceeder.Command += OnCommand;
            CreateCanvas();
            var parent = m_Canvas.RTransform();
            BigDialogViewer.Init(parent);
            ProposalDialogViewer.Init(parent);
            BigDialogViewer.IsOtherDialogViewersShowing = () =>
            {
                var panel = ProposalDialogViewer.CurrentPanel;
                return panel != null && 
                       panel.AppearingState != EAppearingState.Dissapeared;
            };
            ProposalDialogViewer.IsOtherDialogViewersShowing = () =>
            {
                var panel = BigDialogViewer.CurrentPanel;
                return panel != null && 
                       panel.AppearingState != EAppearingState.Dissapeared;
            };
            GameControls.Init();
            DialogPanels.Init();
            RaiseInitializedEvent();
        }

        private void OnCommand(EInputCommand _Key, object[] _Args)
        {
            switch (_Key)
            {
                case EInputCommand.SettingsMenu:
                    DialogPanels.SettingDialogPanel.LoadPanel();
                    BigDialogViewer.Show(DialogPanels.SettingDialogPanel);
                    CommandsProceeder.RaiseCommand(EInputCommand.PauseLevel, null, true);
                    break;
                case EInputCommand.ShopMenu:
                    DialogPanels.ShopMoneyDialogPanel.LoadPanel();
                    BigDialogViewer.Show(DialogPanels.ShopMoneyDialogPanel);
                    CommandsProceeder.RaiseCommand(EInputCommand.PauseLevel, null, true);
                    break;
            }
        }

        public override void OnLevelStageChanged(LevelStageArgs _Args)
        {
            GameControls.OnLevelStageChanged(_Args);
            ShowRateGamePanel(_Args);
        }
        
        #endregion

        #region nonpublic methdos

        private void ShowRateGamePanel(LevelStageArgs _Args)
        {
            if (_Args.Stage != ELevelStage.Finished) 
                return;
            int ratePanelShowsCount = SaveUtils.GetValue(SaveKeys.RatePanelShowsCount);
            bool mustShowRateGamePanel =
                _Args.LevelIndex != 0
                && _Args.LevelIndex % 10 == 0
                && !SaveUtils.GetValue(SaveKeys.GameWasRated)
                && ratePanelShowsCount < 10;
            if (!mustShowRateGamePanel)
                return;
            if (!NetworkUtils.IsInternetConnectionAvailable())
                return;
            Managers.ShopManager.RateGame();
            SaveUtils.PutValue(SaveKeys.RatePanelShowsCount, ratePanelShowsCount + 1);
        }

        #endregion
    }
}