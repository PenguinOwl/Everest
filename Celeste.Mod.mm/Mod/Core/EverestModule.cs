﻿using FMOD.Studio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Celeste.Mod {
    public abstract class EverestModule {

        /// <summary>
        /// Used by Everest itself to store any module metadata.
        /// 
        /// The metadata is usually parsed from meta.yaml in the archive.
        /// 
        /// You can override this property to provide dynamic metadata at runtime.
        /// Note that this doesn't affect mod loading.
        /// </summary>
        public virtual EverestModuleMetadata Metadata { get; set; }

        /// <summary>
        /// Perform any initializing actions after all modd have been loaded.
        /// Do not depend on any specific order in which the mods get initialized.
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// Unload any unmanaged resources allocated by the mod (f.e. textures) and
        /// undo any changes performed by the mod.
        /// </summary>
        public abstract void Unload();

        /// <summary>
        /// Create the mod menu subsection including the section header in the given menu.
        /// </summary>
        /// <param name="menu">Menu to add the section to.</param>
        /// <param name="inGame">Whether we're in-game (paused) or in the main menu.</param>
        /// <param name="snapshot">The Level.PauseSnapshot</param>
        public virtual void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {

        }

    }

    public class EverestModuleMetadata {

        /// <summary>
        /// The path to the ZIP of the mod. In case of unzipped mods, an empty string. Set at runtime.
        /// </summary>
        [YamlIgnore]
        public virtual string PathArchive { get; set; }

        /// <summary>
        /// The path to the directory of the mod. In case of .zips, an empty string. Set at runtime.
        /// </summary>
        [YamlIgnore]
        public virtual string PathDirectory { get; set; }

        /// <summary>
        /// The name of the mod.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The icon of the mod to be used in the mod menu.
        /// Everest loads icon.png by default, but this can also be set by the mod at runtime.
        /// </summary>
        public virtual Texture2D Icon { get; set; }

        /// <summary>
        /// The mod version.
        /// </summary>
        [YamlIgnore]
        public virtual Version Version { get; set; } = new Version(1, 0);
        [YamlMember(Alias = "Version")]
        public string VersionString {
            get {
                return Version.ToString();
            }
            set {
                Version = new Version(value);
            }
        }

        /// <summary>
        /// The path of the mod .dll inside the ZIP or the absolute DLL path if in a directory.
        /// </summary>
        public virtual string DLL { get; set; }

        /// <summary>
        /// Whether the mod has been prelinked or not.
        /// If you don't know what prelinked mods are, don't touch this field.
        /// </summary>
        public virtual bool Prelinked { get; set; } = false;

        /// <summary>
        /// The dependencies of the mod.
        /// </summary>
        public virtual List<EverestModuleMetadata> Dependencies { get; set; } = new List<EverestModuleMetadata>();

        public override string ToString() {
            return Name + " " + Version;
        }

        internal static EverestModuleMetadata Parse(string archive, string directory, StreamReader reader) {
            EverestModuleMetadata meta;
            try {
                meta = YamlHelper.Deserializer.Deserialize<EverestModuleMetadata>(reader);
            } catch (Exception e) {
                Logger.Log("loader", "Failed parsing metadata.yaml: " + e);
                return null;
            }
            if (meta == null) {
                Logger.Log("loader", "Failed parsing metadata.yaml: YamlDotNet returned null");
                return null;
            }
            meta.PathArchive = archive;
            meta.PathDirectory = directory;

            if (!string.IsNullOrEmpty(directory))
                meta.DLL = Path.Combine(directory, meta.DLL.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));

            // Add dependency to API 1.0 if missing.
            bool dependsOnAPI = false;
            foreach (EverestModuleMetadata dep in meta.Dependencies) {
                if (dep.Name == "API")
                    dep.Name = "Everest";
                if (dep.Name == "Everest") {
                    dependsOnAPI = true;
                    break;
                }
            }
            if (!dependsOnAPI) {
                Logger.Log("loader", "WARNING: No dependency to API found in " + meta + "! Adding dependency to API 1.0...");
                meta.Dependencies.Insert(0, new EverestModuleMetadata() {
                    Name = "API",
                    Version = new Version(1, 0)
                });
            }

            return meta;
        }

    }
}
