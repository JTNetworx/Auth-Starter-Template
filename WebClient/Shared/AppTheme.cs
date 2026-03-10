using MudBlazor;

namespace WebClient.Shared;

public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            // Orange primary, Blue secondary, Blue-gray accents
            Primary = "#F57C00",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#E65100",
            PrimaryLighten = "#FFB74D",

            Secondary = "#1976D2",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#1565C0",
            SecondaryLighten = "#64B5F6",

            Tertiary = "#78909C",            // Blue-gray / Silver
            TertiaryContrastText = "#FFFFFF",

            Background = "#F5F5F5",          // Light gray background
            Surface = "#FFFFFF",
            DrawerBackground = "#ECEFF1",    // Silver-gray drawer
            DrawerText = "#37474F",

            AppbarBackground = "#263238",    // Dark blue-gray appbar
            AppbarText = "#ECEFF1",

            TextPrimary = "#212121",
            TextSecondary = "#616161",
            TextDisabled = "#9E9E9E",

            ActionDefault = "#757575",
            ActionDisabled = "#BDBDBD",
            ActionDisabledBackground = "#E0E0E0",

            Divider = "#E0E0E0",
            DividerLight = "#F5F5F5",

            Success = "#388E3C",
            Warning = "#F57C00",
            Error = "#D32F2F",
            Info = "#1976D2",
        },

        PaletteDark = new PaletteDark
        {
            Primary = "#FF8F00",             // Amber-orange
            PrimaryContrastText = "#000000",
            PrimaryDarken = "#E65100",
            PrimaryLighten = "#FFCA28",

            Secondary = "#42A5F5",           // Light blue
            SecondaryContrastText = "#000000",
            SecondaryDarken = "#1E88E5",
            SecondaryLighten = "#90CAF9",

            Tertiary = "#90A4AE",            // Silver-blue
            TertiaryContrastText = "#000000",

            Background = "#121212",
            Surface = "#1E1E1E",
            DrawerBackground = "#1A1A1A",
            DrawerText = "#ECEFF1",

            AppbarBackground = "#0D0D0D",
            AppbarText = "#ECEFF1",

            TextPrimary = "#ECEFF1",
            TextSecondary = "#B0BEC5",
            TextDisabled = "#546E7A",

            ActionDefault = "#90A4AE",
            ActionDisabled = "#546E7A",
            ActionDisabledBackground = "#263238",

            Divider = "#37474F",
            DividerLight = "#263238",

            Success = "#66BB6A",
            Warning = "#FFA726",
            Error = "#EF5350",
            Info = "#42A5F5",
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "240px",
            AppbarHeight = "64px"
        }
    };
}
