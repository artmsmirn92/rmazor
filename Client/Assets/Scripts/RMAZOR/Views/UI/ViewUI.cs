﻿using System.Collections.Generic;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.UI.DialogViewers;
using mazing.common.Runtime.Utils;
using RMAZOR.Models;
using RMAZOR.UI.Panels;
using RMAZOR.UI.Panels.ShopPanels;
using RMAZOR.Views.Common;
using RMAZOR.Views.InputConfigurators;

namespace RMAZOR.Views.UI
{
    public class ViewUI : ViewUIBase
    {
        #region inject

        private IDialogViewersController            DialogViewersController        { get; }
        private IDialogPanelsSet                    DialogPanelsSet                { get; }
        private IViewInputCommandsProceeder         CommandsProceeder              { get; }
        private IViewUIRateGamePanelController      RateGamePanelController        { get; }
        private IViewSwitchLevelStageCommandInvoker SwitchLevelStageCommandInvoker { get; }
        private IViewTimePauser                     TimePauser                     { get; }

        private ViewUI(
            IDialogViewersController            _DialogViewersController,
            IViewUIGameControls                 _GameControls,
            IDialogPanelsSet                    _DialogPanelsSet,
            IViewInputCommandsProceeder         _CommandsProceeder,
            IViewUIRateGamePanelController      _RateGamePanelController,
            IViewSwitchLevelStageCommandInvoker _SwitchLevelStageCommandInvoker,
            IViewTimePauser                     _TimePauser)
            : base(_GameControls)
        {
            DialogViewersController        = _DialogViewersController;
            DialogPanelsSet                = _DialogPanelsSet;
            CommandsProceeder              = _CommandsProceeder;
            RateGamePanelController        = _RateGamePanelController;
            SwitchLevelStageCommandInvoker = _SwitchLevelStageCommandInvoker;
            TimePauser                     = _TimePauser;
        }

        #endregion
        
        #region api

        public override void Init()
        {
            CommandsProceeder.Command += OnCommand;
            GameControls.Init();
            DialogViewersController.Init();
            RateGamePanelController.Init();
            Cor.Run(Cor.WaitNextFrame(() => DialogPanelsSet.Init()));
            base.Init();
        }
        
        private void OnCommand(EInputCommand _Key, Dictionary<string, object> _Args)
        {
            switch (_Key)
            {
                case EInputCommand.SettingsPanel:
                {
                    var panel = DialogPanelsSet.GetPanel<ISettingDialogPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    dv.Show(panel);
                    SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                }
                    break;
                case EInputCommand.ShopPanel:
                {
                    var panel = DialogPanelsSet.GetPanel<IShopDialogPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    panel.SetOnCloseFinishAction(() =>
                    {
                        object loadShopPanelFromCharDiedPanelArg1 = _Args.GetSafe(
                            CommonInputCommandArg.KeyLoadShopPanelFromCharacterDiedPanel, out bool keyExist1);
                        if (keyExist1 && (bool)loadShopPanelFromCharDiedPanelArg1)
                        {
                            DialogPanelsSet.GetPanel<ICharacterDiedDialogPanel>().ReturnFromShopPanel();
                        }
                        else
                        {
                            TimePauser.UnpauseTimeInGame();
                            SwitchLevelStageCommandInvoker.SwitchLevelStage(
                                EInputCommand.UnPauseLevel);
                        }
                    });
                    dv.Show(panel);
                    object loadShopPanelFromCharDiedPanelArg = _Args.GetSafe(
                        CommonInputCommandArg.KeyLoadShopPanelFromCharacterDiedPanel, out bool keyExist);
                    if (!keyExist || (bool)loadShopPanelFromCharDiedPanelArg == false)
                        SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                }
                    break;
                case EInputCommand.DailyGiftPanel:
                {
                    SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                    var panel = DialogPanelsSet.GetPanel<IDailyGiftPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    dv.Show(panel);
                }
                    break;
                case EInputCommand.LevelsPanel:
                {
                    
                    SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                    var panel = DialogPanelsSet.GetPanel<ILevelsDialogPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    dv.Show(panel);
                }
                    break;
                case EInputCommand.PlayBonusLevelPanel:
                {
                    SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                    var panel = DialogPanelsSet.GetPanel<IPlayBonusLevelDialogPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    dv.Show(panel);
                }
                    break;
                case EInputCommand.FinishLevelGroupPanel:
                {
                    SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                    var panel = DialogPanelsSet.GetPanel<IFinishLevelGroupDialogPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    dv.Show(panel);
                }
                    break;
                case EInputCommand.TutorialPanel:
                {
                    SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                    var panel = DialogPanelsSet.GetPanel<ITutorialDialogPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    dv.Show(panel, 3f);
                }
                    break;
                case EInputCommand.DisableAdsPanel:
                {
                    SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.PauseLevel);
                    var panel = DialogPanelsSet.GetPanel<IDisableAdsDialogPanel>();
                    var dv = DialogViewersController.GetViewer(panel.DialogViewerType);
                    dv.Show(panel, 3f);
                }
                    break;
            }
        }

        public override void OnLevelStageChanged(LevelStageArgs _Args)
        {
            RateGamePanelController.OnLevelStageChanged(_Args);
            GameControls.OnLevelStageChanged(_Args);
        }

        #endregion
    }
}