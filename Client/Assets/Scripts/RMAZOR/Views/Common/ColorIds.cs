﻿using System.Collections.Generic;
using Common.Extensions;

namespace RMAZOR.Views.Common
{
    public static class ColorIds
    {
        public static readonly int Main;
        public static readonly int Background;
        public static readonly int BackgroundIdleItems;
        public static readonly int Character;
        public static readonly int CharacterTail;
        public static readonly int MazeItem1;
        public static readonly int MazeItem2;
        public static readonly int MoneyItem;
        public static readonly int UI;
        public static readonly int UiDialogItemNormal;
        public static readonly int UiDialogBackground;
        public static readonly int UiBorder;
        public static readonly int UiText;
        public static readonly int UiBackground;
        public static readonly int UiItemHighlighted;
        public static readonly int UiStartLogo;

        static ColorIds()
        {
            Main                     = GetHash(nameof(Main));
            Background               = GetHash(nameof(Background));
            BackgroundIdleItems      = GetHash(nameof(BackgroundIdleItems)     .WithSpaces());
            Character                = GetHash(nameof(Character));
            CharacterTail            = GetHash(nameof(CharacterTail)           .WithSpaces());
            MazeItem1                = GetHash(nameof(MazeItem1)               .WithSpaces());
            MazeItem2                = GetHash(nameof(MazeItem2)               .WithSpaces());
            MoneyItem                = GetHash(nameof(MoneyItem)               .WithSpaces());
            UI                       = GetHash(nameof(UI));
            UiDialogItemNormal       = GetHash(nameof(UiDialogItemNormal)      .WithSpaces());
            UiDialogBackground       = GetHash(nameof(UiDialogBackground)      .WithSpaces());
            UiBorder                 = GetHash(nameof(UiBorder)                .WithSpaces());
            UiText                   = GetHash(nameof(UiText)                  .WithSpaces());
            UiBackground             = GetHash(nameof(UiBackground)            .WithSpaces());
            UiItemHighlighted        = GetHash(nameof(UiItemHighlighted)       .WithSpaces());
            UiStartLogo              = GetHash(nameof(UiStartLogo)             .WithSpaces());
        }

        public static int GetHash(string _S)
        {
            return _S.GetHashCode();
        }

        public static string GetColorNameById(int _Id)
        {
            return ColorNamesDict[_Id];
        }
        
        private static Dictionary<int, string> ColorNamesDict => new Dictionary<int, string>
        {
            {Main,                     nameof(Main).WithSpaces()},
            {Background,               nameof(Background).WithSpaces()},
            {BackgroundIdleItems,      nameof(BackgroundIdleItems).WithSpaces()},
            {Character,                nameof(Character).WithSpaces()},
            {CharacterTail,            nameof(CharacterTail).WithSpaces()},
            {MazeItem1,                nameof(MazeItem1).WithSpaces()},
            {MazeItem2,                nameof(MazeItem2).WithSpaces()},
            {MoneyItem,                nameof(MoneyItem).WithSpaces()},
            {UI,                       nameof(UI)},
            {UiDialogItemNormal,       nameof(UiDialogItemNormal).WithSpaces()},
            {UiDialogBackground,       nameof(UiDialogBackground).WithSpaces()},
            {UiBorder,                 nameof(UiBorder).WithSpaces()},
            {UiText,                   nameof(UiText).WithSpaces()},
            {UiBackground,             nameof(UiBackground).WithSpaces()},
            {UiItemHighlighted,        nameof(UiItemHighlighted).WithSpaces()},
            {UiStartLogo,              nameof(UiStartLogo).WithSpaces()}
        };
    }
}