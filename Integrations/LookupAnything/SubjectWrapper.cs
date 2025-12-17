// File: Integrations/LookupAnything/SubjectWrapper.cs

using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUISupport.Framework;

namespace MobileUISupport.Integrations.LookupAnything
{
    /// <summary>
    /// Wrapper untuk ISubject dari LookupAnything.
    /// Mengakses properties via reflection karena ISubject adalah internal.
    /// </summary>
    public class SubjectWrapper
    {
        // ═══════════════════════════════════════════════════════
        // Fields
        // ═══════════════════════════════════════════════════════

        private readonly object _subject;
        private readonly Type _subjectType;

        // Cached PropertyInfo
        private readonly PropertyInfo? _nameProperty;
        private readonly PropertyInfo? _descriptionProperty;
        private readonly PropertyInfo? _typeProperty;
        private readonly MethodInfo? _drawPortraitMethod;

        // ═══════════════════════════════════════════════════════
        // Properties
        // ═══════════════════════════════════════════════════════

        /// <summary>Object ISubject asli untuk dikirim ke ShowLookupFor.</summary>
        public object RawSubject => _subject;

        /// <summary>Nama subject (untuk display di search results).</summary>
        public string Name { get; }

        /// <summary>Deskripsi subject.</summary>
        public string Description { get; }

        /// <summary>Tipe subject (Villager, Item, Building, dll).</summary>
        public string SubjectType { get; }

        /// <summary>Apakah wrapper valid.</summary>
        public bool IsValid { get; }

        // ═══════════════════════════════════════════════════════
        // Constructor
        // ═══════════════════════════════════════════════════════

        private SubjectWrapper(object subject)
        {
            _subject = subject;
            _subjectType = subject.GetType();

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Cache property info berdasarkan ISubject interface
            _nameProperty = _subjectType.GetProperty("Name", flags);
            _descriptionProperty = _subjectType.GetProperty("Description", flags);
            _typeProperty = _subjectType.GetProperty("Type", flags);
            _drawPortraitMethod = _subjectType.GetMethod("DrawPortrait", flags);

            // Read values
            Name = GetPropertyValue<string>(_nameProperty) ?? "Unknown";
            Description = GetPropertyValue<string>(_descriptionProperty) ?? "";
            SubjectType = GetPropertyValue<string>(_typeProperty) ?? "Other";

            IsValid = _nameProperty != null;
        }

        // ═══════════════════════════════════════════════════════
        // Private Methods
        // ═══════════════════════════════════════════════════════

        private T? GetPropertyValue<T>(PropertyInfo? property)
        {
            if (property == null) return default;

            try
            {
                var value = property.GetValue(_subject);
                if (value is T typedValue)
                    return typedValue;
                return default;
            }
            catch
            {
                return default;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Draw portrait/icon untuk subject.
        /// </summary>
        public bool DrawPortrait(SpriteBatch spriteBatch, Vector2 position, Vector2 size)
        {
            if (_drawPortraitMethod == null)
                return false;

            try
            {
                // Method signature: void DrawPortrait(SpriteBatch spriteBatch, Vector2 position, Vector2 size)
                _drawPortraitMethod.Invoke(_subject, new object[] { spriteBatch, position, size });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Map SubjectType ke kategori untuk filtering.
        /// </summary>
        public string GetCategory()
        {
            return SubjectType?.ToLowerInvariant() switch
            {
                "villager" or "npc" or "character" or "pet" or "child" => "NPCs",
                "farmer" or "player" => "NPCs",
                "item" or "object" or "tool" or "weapon" or "ring" or "boots" or "hat" or "clothing" => "Items",
                "crop" or "tree" or "fruittree" or "bush" => "Crops",
                "building" or "structure" => "Buildings",
                "terrainfeature" or "resourceclump" => "Terrain",
                "monster" or "creature" => "Monsters",
                "farmanimals" or "animal" => "Animals",
                _ => "Other"
            };
        }

        // ═══════════════════════════════════════════════════════
        // Static Factory
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Buat SubjectWrapper dari object.
        /// Return null jika gagal.
        /// </summary>
        public static SubjectWrapper? Create(object? subject)
        {
            if (subject == null) return null;

            try
            {
                var wrapper = new SubjectWrapper(subject);
                return wrapper.IsValid ? wrapper : null;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Failed to create SubjectWrapper: {ex.Message}");
                return null;
            }
        }
    }
}