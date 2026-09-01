using DeadSignal.Combat;
using DeadSignal.Missions;
using UnityEngine;
using UnityEngine.UI;

namespace DeadSignal.Presentation
{
    /// <summary>Turns authoritative specialist state into a compact, presentation-only threat instrument.</summary>
    public sealed class ThreatHudInstrument : MonoBehaviour
    {
        private const float PURGE_NOTICE_SECONDS = 1.25f;

        [SerializeField] private Image m_background;
        [SerializeField] private Image m_accent;
        [SerializeField] private Image m_healthFill;
        [SerializeField] private Text m_header;
        [SerializeField] private Text m_role;
        [SerializeField] private Text m_state;
        [SerializeField] private Text m_health;
        [SerializeField] private Text m_footer;
        [SerializeField] private CanvasGroup m_canvasGroup;

        private bool m_initialized;
        private bool m_wardenAlive;
        private bool m_sapperAlive;
        private bool m_interceptorAlive;
        private bool m_suppressorAlive;
        private float m_purgeNoticeRemaining;
        private string m_purgeNotice = string.Empty;

        public bool IsConfigured => m_background != null && m_accent != null && m_healthFill != null &&
                                    m_header != null && m_role != null && m_state != null && m_health != null &&
                                    m_footer != null && m_canvasGroup != null;
        public string HeaderLabel => m_header == null ? string.Empty : m_header.text;
        public string RoleLabel => m_role == null ? string.Empty : m_role.text;
        public string StateLabel => m_state == null ? string.Empty : m_state.text;
        public string HealthLabel => m_health == null ? string.Empty : m_health.text;
        public string FooterLabel => m_footer == null ? string.Empty : m_footer.text;
        public float HealthRatio => m_healthFill == null ? 0f : m_healthFill.fillAmount;

        public void Configure(
            Image background,
            Image accent,
            Image healthFill,
            Text header,
            Text role,
            Text state,
            Text health,
            Text footer,
            CanvasGroup canvasGroup)
        {
            m_background = background;
            m_accent = accent;
            m_healthFill = healthFill;
            m_header = header;
            m_role = role;
            m_state = state;
            m_health = health;
            m_footer = footer;
            m_canvasGroup = canvasGroup;
        }

        internal void Apply(
            RunModel model,
            DeadSignalThreatController threats,
            bool pursuit,
            bool reducedFlashes,
            float deltaTime)
        {
            if (!IsConfigured || model == null || threats == null)
            {
                return;
            }

            _updatePurgeNotice(threats, deltaTime);
            m_header.text = pursuit
                ? "SECURITY  //  PURSUIT"
                : $"SECURITY  //  ALERT {threats.EscalationTier}/{RunModel.SalvageRequired}";

            if (m_purgeNoticeRemaining > 0f)
            {
                _applyPurgeNotice(reducedFlashes);
                return;
            }

            if (!model.TowerOnline)
            {
                _applyDormant(threats);
                return;
            }

            if (threats.IsSapperAlive)
            {
                _applyThreat(
                    "SAPPER  //  DENIAL",
                    threats.IsSapperLatched ? $"!! DRAIN PULSE  {threats.SapperPulseCooldown:0.0}s" : "▲ CLOSING",
                    threats.SapperHealth,
                    threats.SapperMaximumHealth,
                    threats.SapperSignalReward,
                    threats.IsSapperLatched,
                    reducedFlashes,
                    new Color(1f, 0.16f, 0.62f, 1f));
                return;
            }

            if (threats.IsInterceptorAlive)
            {
                var state = threats.IsInterceptorRecovering
                    ? $"◆ EXPOSED  {threats.InterceptorRecoverySecondsRemaining:0.0}s"
                    : threats.IsInterceptorCharging
                        ? "!! DASH LOCK"
                        : threats.IsInterceptorCuttingSapperFlank ? "▲ CUTTING FLANK" : "▲ FLANKING";
                _applyThreat(
                    "INTERCEPTOR  //  PURSUIT",
                    state,
                    threats.InterceptorHealth,
                    threats.InterceptorMaximumHealth,
                    threats.InterceptorSignalReward,
                    threats.IsInterceptorCharging,
                    reducedFlashes,
                    new Color(1f, 0.25f, 0.18f, 1f));
                return;
            }

            if (threats.IsSuppressorAlive)
            {
                var imminent = threats.IsSuppressorFieldWarningActive || threats.IsSuppressorFieldActive;
                _applyThreat(
                    "SUPPRESSOR  //  DENIAL",
                    threats.IsSuppressorFieldActive ? "!! FIELD ACTIVE" :
                        threats.IsSuppressorFieldWarningActive ? "!! FIELD WARNING" : "▲ POSITIONING",
                    threats.SuppressorHealth,
                    threats.SuppressorMaximumHealth,
                    threats.SuppressorSignalReward,
                    imminent,
                    reducedFlashes,
                    new Color(1f, 0.18f, 0.54f, 1f));
                return;
            }

            if (threats.IsWardenAlive)
            {
                _applyThreat(
                    "WARDEN  //  PRESSURE",
                    threats.IsWardenScreeningSapper ? "!! SCREENING SAPPER" : "▲ PURSUING",
                    threats.WardenHealth,
                    threats.WardenMaximumHealth,
                    threats.WardenSignalReward,
                    threats.IsWardenScreeningSapper,
                    reducedFlashes,
                    new Color(1f, 0.31f, 0.16f, 1f));
                return;
            }

            m_role.text = "LOCAL SECURITY CLEAR";
            m_state.text = "◆ NO SPECIALIST CONTACT";
            m_health.text = "HP —";
            m_healthFill.fillAmount = 0f;
            m_accent.color = new Color(0.08f, 0.9f, 1f, 1f);
            m_footer.text = _footer(threats, 0f);
            m_background.color = new Color(0.012f, 0.028f, 0.036f, 0.94f);
            m_canvasGroup.alpha = 0.9f;
            transform.localScale = Vector3.one;
        }

        private void _updatePurgeNotice(DeadSignalThreatController threats, float deltaTime)
        {
            if (!m_initialized)
            {
                m_initialized = true;
                m_wardenAlive = threats.IsWardenAlive;
                m_sapperAlive = threats.IsSapperAlive;
                m_interceptorAlive = threats.IsInterceptorAlive;
                m_suppressorAlive = threats.IsSuppressorAlive;
                return;
            }

            if (m_sapperAlive && !threats.IsSapperAlive)
                _setPurgeNotice("SAPPER PURGED", threats.SapperSignalReward);
            else if (m_interceptorAlive && !threats.IsInterceptorAlive)
                _setPurgeNotice("INTERCEPTOR PURGED", threats.InterceptorSignalReward);
            else if (m_suppressorAlive && !threats.IsSuppressorAlive)
                _setPurgeNotice("SUPPRESSOR PURGED", threats.SuppressorSignalReward);
            else if (m_wardenAlive && !threats.IsWardenAlive)
                _setPurgeNotice("WARDEN PURGED", threats.WardenSignalReward);

            m_wardenAlive = threats.IsWardenAlive;
            m_sapperAlive = threats.IsSapperAlive;
            m_interceptorAlive = threats.IsInterceptorAlive;
            m_suppressorAlive = threats.IsSuppressorAlive;
            m_purgeNoticeRemaining = Mathf.Max(0f, m_purgeNoticeRemaining - Mathf.Max(0f, deltaTime));
        }

        private void _setPurgeNotice(string label, float reward)
        {
            m_purgeNotice = $"{label}  //  +{reward:0} SIGNAL";
            m_purgeNoticeRemaining = PURGE_NOTICE_SECONDS;
        }

        private void _applyPurgeNotice(bool reducedFlashes)
        {
            m_role.text = m_purgeNotice;
            m_state.text = "◆ THREAT REMOVED";
            m_health.text = "CLEAR";
            m_healthFill.fillAmount = 1f;
            m_healthFill.color = new Color(0.08f, 0.9f, 1f, 0.9f);
            m_accent.color = new Color(0.08f, 0.9f, 1f, 1f);
            m_footer.text = "SIGNAL RECLAIM CONFIRMED";
            m_background.color = new Color(0.01f, 0.07f, 0.085f, 0.96f);
            m_canvasGroup.alpha = reducedFlashes ? 0.96f : Mathf.Lerp(0.82f, 1f, m_purgeNoticeRemaining / PURGE_NOTICE_SECONDS);
            transform.localScale = Vector3.one;
        }

        private void _applyDormant(DeadSignalThreatController threats)
        {
            m_header.text = "SECURITY  //  DORMANT";
            m_role.text = "SPECIALIST NETWORK OFFLINE";
            m_state.text = "— RESTORE CENTRAL TO WAKE";
            m_health.text = "4 ROLES";
            m_healthFill.fillAmount = 0f;
            m_accent.color = new Color(0.35f, 0.48f, 0.52f, 1f);
            m_footer.text = $"BOUNTIES  W+{threats.WardenSignalReward:0}  S+{threats.SapperSignalReward:0}  " +
                            $"I+{threats.InterceptorSignalReward:0}  X+{threats.SuppressorSignalReward:0}";
            m_background.color = new Color(0.012f, 0.028f, 0.036f, 0.9f);
            m_canvasGroup.alpha = 0.82f;
            transform.localScale = Vector3.one;
        }

        private void _applyThreat(
            string role,
            string state,
            float health,
            float maximumHealth,
            float reward,
            bool imminent,
            bool reducedFlashes,
            Color accent)
        {
            var ratio = maximumHealth > 0f ? Mathf.Clamp01(health / maximumHealth) : 0f;
            m_role.text = role;
            m_state.text = state;
            m_health.text = $"HP {Mathf.CeilToInt(health):00}/{Mathf.CeilToInt(maximumHealth):00}";
            m_healthFill.fillAmount = ratio;
            m_healthFill.color = accent;
            m_accent.color = accent;
            m_footer.text = _footer(null, reward);
            m_background.color = imminent
                ? new Color(0.13f, 0.012f, 0.026f, 0.97f)
                : new Color(0.018f, 0.028f, 0.04f, 0.94f);
            m_canvasGroup.alpha = 1f;
            var pulse = imminent && !reducedFlashes ? 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.018f : 1f;
            transform.localScale = Vector3.one * pulse;
        }

        private static string _footer(DeadSignalThreatController threats, float reward)
        {
            if (threats == null)
            {
                return $"PURGE RECLAIMS  +{reward:0} SIGNAL";
            }

            var entry = threats.PendingReinforcement == SecurityReinforcement.None
                ? string.Empty
                : threats.IsReinforcementEntryBlocked
                    ? $"  //  {threats.PendingReinforcement.ToString().ToUpperInvariant()} GATE BLOCKED"
                    : $"  //  {threats.PendingReinforcement.ToString().ToUpperInvariant()} ENTRY " +
                      $"{threats.ReinforcementEntryCountdown:0.0}s";
            return $"RESERVE  {threats.ReinforcementsRemaining}{entry}";
        }
    }
}
