// Copyright (c) 2024 Augie R. Maddox, Guavaman Enterprises. All rights reserved.

#pragma warning disable 0649

namespace Rewired.Glyphs {
    using Rewired;

    /// <summary>
    /// Options for Controller Element Glyph selection.
    /// </summary>
    [System.Serializable]
    public class ControllerElementGlyphSelectorOptions {

        [UnityEngine.Tooltip("Determines if the Player's last active controller is used for glyph selection.")]
        [UnityEngine.SerializeField]
        private bool _useLastActiveController = true;

        [UnityEngine.Tooltip(
            "List of controller type priority. " +
            "First in list corresponds to highest priority. " +
            "This determines which controller types take precedence when displaying glyphs. " +
            "If use last active controller is enabled, the active controller will always take priority, " + 
            "however, if there is no last active controller, selection will fall back based on this priority. " +
            "In addition, keyboard and mouse are treated as a single controller for the purposes of glyph handling, " +
            "so to prioritze keyboard over mouse or vice versa, the one that is lower in the list will take precedence."
        )]
        [UnityEngine.SerializeField]
        private ControllerType[] _controllerTypeOrder = new ControllerType[] {
            ControllerType.Joystick,
            ControllerType.Custom,
            ControllerType.Mouse,
            ControllerType.Keyboard
        };

        [System.NonSerialized]
        private System.Predicate<Rewired.ActionElementMap> _isActionElementMapAllowedHandler;

        /// <summary>
        /// Determines if the Player's last active controller is used for glyph selection.
        /// </summary>
        public bool useLastActiveController {
            get {
                return _useLastActiveController;
            }
             set {
                _useLastActiveController = value;
            }
        }

        /// <summary>
        /// List of controller type priority.
        /// First in list corresponds to highest priority.
        /// This determines which controller types take precedence when displaying glyphs.
        /// If use last active controller is enabled, the active controller will always take priority,
        /// however, if there is no last active controller, selection will fall back based on this priority.
        /// In addition, keyboard and mouse are treated as a single controller for the purposes of glyph handling,
        /// so to prioritze keyboard over mouse or vice versa, the one that is lower in the list will take precedence.
        /// </summary>
        public ControllerType[] controllerTypeOrder {
            get {
                return _controllerTypeOrder;
            }
            set {
                _controllerTypeOrder = value;
            }
        }

        /// <summary>
        /// Allows you to filter which Action Element Maps are displayed.
        /// When searching for mappings in the Player, the handler will be invoked for each Action Element Map found.
        /// This allows you to, for example, allow only Action Element Maps belonging to a Controller Map in particular Map Category.
        /// <see cref="Rewired.ActionElementMap"/> for properties which can be used as filtering criteria.
        /// IMPORTANT: By setting this value, you are taking over the responsibility for filtering mappings results entirely. The default
        /// filter removes disabled Action Element Maps and mappings in disabled Controller Maps. Disabled mappings are no longer removed
        /// when this handler is overriden with your own, so you must check the enabled states if you want to exclude these mappings.
        public System.Predicate<Rewired.ActionElementMap> isActionElementMapAllowedHandler {
            get {
                return _isActionElementMapAllowedHandler;
            }
            set {
                _isActionElementMapAllowedHandler = value;
            }
        }

        /// <summary>
        /// Gets the controller type priority for the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="controllerType">The result.</param>
        /// <returns>True if the controller type was found, false if not.</returns>
        public virtual bool TryGetControllerTypeOrder(int index, out ControllerType controllerType) {
            if ((uint)index >= (uint)_controllerTypeOrder.Length) {
                controllerType = ControllerType.Keyboard;
                return false;
            }
            controllerType = _controllerTypeOrder[index];
            return true;
        }

        private static ControllerElementGlyphSelectorOptions s_defaultOptions;
        /// <summary>
        /// The default options.
        /// Set this to override the default selector options.
        /// </summary>
        public static ControllerElementGlyphSelectorOptions defaultOptions {
            get {
                return s_defaultOptions != null ? s_defaultOptions : s_defaultOptions = new ControllerElementGlyphSelectorOptions();
            }
            set {
                s_defaultOptions = value;
            }
        }
    }
}
