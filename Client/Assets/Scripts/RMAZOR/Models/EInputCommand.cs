﻿namespace RMAZOR.Models
{
    public enum EInputCommand
    {
        // move commands
        MoveUp = 0,
        MoveDown,
        MoveLeft,
        MoveRight,
        // rotate commands
        RotateClockwise,
        RotateCounterClockwise,
        // level staging commands
        LoadLevel,
        ReadyToStartLevel,
        StartOrContinueLevel,
        FinishLevel,
        StartUnloadingLevel,
        UnloadLevel,
        PauseLevel,
        UnPauseLevel,
        KillCharacter,
        ExitLevelStaging,
        // ui commands
        ShopMoneyPanel,
        ShopPanel,
        DisableAdsPanel,
        SettingsPanel,
        DailyGiftPanel,
        LevelsPanel,
        RateGamePanel,
        FinishLevelGroupPanel,
        PlayBonusLevelPanel,
        MainMenuPanel,
        RateGameFromGameUi,
        DailyChallengePanel,
        HintPanel,
        CustomizeCharacterPanel,
        ConfirmGoToMainMenuPanel,
        // debug and prototyping commands
        EnableDebug,
        DisableDebug,
        StartRecordCommands,
        StopRecordCommands,
        GetRecordedCommands,
        PlayRecordedCommands,
        SetRecordCommadsFromClipboard,
        SelectCharacter,
        SelectCharacterColor,
    }
}