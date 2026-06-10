window.getAvatar = function (user, size) {

    if (user.defaultAvatarValue !== null && user.defaultAvatarValue !== undefined) {
        return `./publicResources/defaultAvatars/${user.defaultAvatarValue}`;
    }

    if (!user.profileImageUrl) {
        return './publicResources/defaultAvatars/domyslne.png';
    }

    const baseUrl = "https://matchikomapstorage.blob.core.windows.net/profile-images";

    const normalizedSize =
        size === "100" ? "100x100" :
            size === "300" ? "300x300" :
                size === "600" ? "600x600" :
                    "original";

    return `${baseUrl}/${normalizedSize}/${user.profileImageUrl}`;
};