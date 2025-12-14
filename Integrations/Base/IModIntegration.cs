namespace MobileUISupport.Integrations.Base
{
    /// <summary>
    /// Interface untuk semua mod integrations.
    /// Memudahkan penambahan integration baru.
    /// </summary>
    public interface IModIntegration
    {
        /// <summary>
        /// Unique ID mod yang di-integrate.
        /// </summary>
        string ModId { get; }

        /// <summary>
        /// Display name untuk logging.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Apakah mod tersedia dan berhasil di-initialize.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Apakah integration ini enabled di config.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Initialize integration. Return true jika berhasil.
        /// </summary>
        bool Initialize();

        /// <summary>
        /// Called saat save loaded.
        /// </summary>
        void OnSaveLoaded();

        /// <summary>
        /// Called saat returned to title.
        /// </summary>
        void OnReturnedToTitle();
    }
}