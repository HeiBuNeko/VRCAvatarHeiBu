using VRC.SDK3.Avatars.Components;

namespace io.github.azukimochi;

internal sealed class AvatarParameterDriverCreationFailedExeption : Exception
{
}

internal static class AvatarParameterDriverCreationFailedExeptionExt
{
    /// <exception cref="AvatarParameterDriverCreationFailedExeption"/>
    public static VRCAvatarParameterDriver ThrowIfNull(this VRCAvatarParameterDriver driver)
    {
        if (driver != null)
            return driver;
        throw new AvatarParameterDriverCreationFailedExeption();
    }
}

