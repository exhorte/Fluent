namespace Fluent.App.Localization;

/// <summary>English/French catalog for the interface. Default language: English.</summary>
internal static class LocalizedStrings
{
    public static IReadOnlyDictionary<string, (string En, string Fr)> Catalog { get; } =
        new Dictionary<string, (string En, string Fr)>
        {
            // Navigation
            ["nav.section"] = ("LOCAL SPACE", "ESPACE LOCAL"),
            ["nav.home"] = ("Home", "Accueil"),
            ["nav.history"] = ("History", "Historique"),
            ["nav.dictionary"] = ("Dictionary", "Dictionnaire"),
            ["nav.subscription"] = ("Subscription", "Abonnement"),
            ["nav.settings"] = ("Settings", "Paramètres"),
            ["nav.upgrade"] = ("Upgrade plan", "Passer à Pro"),
            ["chip.localUser"] = ("Local user", "Utilisateur local"),

            // Home (overview)
            ["home.title"] = ("Home", "Accueil"),
            ["home.subtitle"] = (
                "Your local dictation, its status and its protections at a glance.",
                "Votre dictée locale, son état et ses protections en un coup d'œil."),
            ["home.card.state"] = ("STATE", "ÉTAT"),
            ["home.state.desc"] = ("Current dictation", "Dictée courante"),
            ["home.card.shortcut"] = ("SHORTCUT", "RACCOURCI"),
            ["home.shortcut.value"] = ("Ctrl + Win — hold to talk", "Ctrl + Win — maintenir pour parler"),
            ["home.card.engine"] = ("ENGINE", "MOTEUR"),
            ["home.engine.desc"] = ("Local Whisper · CPU", "Whisper local · CPU"),
            ["home.card.privacy"] = ("PRIVACY", "CONFIDENTIALITÉ"),
            ["home.privacy.value"] = ("In memory", "En mémoire"),
            ["home.privacy.desc"] = ("No audio file", "Aucun fichier audio"),
            ["home.lastActivity"] = ("Last activity", "Dernière activité"),
            ["home.lastResult.default"] = (
                "Place the caret in a text field, then hold Ctrl+Win to dictate. Release to transcribe and insert automatically.",
                "Placez le curseur dans un champ texte, puis maintenez Ctrl+Win pour dicter. Relâchez pour transcrire et insérer automatiquement."),
            ["home.insertion.note"] = (
                "Insertion only if the initial target stays valid; otherwise, explicit copy to the clipboard. Local history is off by default (opt-in).",
                "Insertion seulement si la cible initiale reste valide ; sinon, copie explicite dans le presse-papiers. L'historique local est désactivé par défaut (opt-in)."),
            ["home.target.title"] = ("Secure target", "Cible sécurisée"),
            ["home.target.none"] = ("No locked target", "Aucune cible verrouillée"),
            ["home.target.note"] = (
                "Password field blocked · target re-checked before insertion",
                "Champ secret bloqué · cible revérifiée avant insertion"),
            ["home.foundations.title"] = ("Product foundations", "Fondations du produit"),
            ["home.foundations.subtitle"] = (
                "The dashboard is ready for upcoming features without showing fake data.",
                "Le dashboard est prêt à accueillir les prochaines fonctions sans afficher de données fictives."),

            // History
            ["history.title"] = ("History", "Historique"),
            ["history.subtitle"] = (
                "Local, optional history of your dictations. No audio, nothing sent outside.",
                "Historique local et optionnel de vos dictées. Aucun audio, aucun envoi externe."),
            ["history.recording.title"] = ("History recording", "Enregistrement de l'historique"),
            ["history.recording.desc"] = (
                "Off by default. When enabled, only the final text of your dictations is stored, on this device only. No audio is ever kept and nothing is sent outside. The 500 most recent entries are kept; you can delete one entry or clear everything at any time.",
                "Désactivé par défaut. Lorsqu'il est activé, seul le texte final de vos dictées est enregistré, uniquement sur cet appareil. Aucun audio n'est jamais conservé et rien n'est envoyé à l'extérieur. Les 500 entrées les plus récentes sont conservées ; vous pouvez supprimer une entrée ou tout effacer à tout moment."),
            ["history.stored.title"] = ("Stored dictations", "Dictées enregistrées"),
            ["history.search"] = ("SEARCH", "RECHERCHER"),
            ["history.clearAll"] = ("Clear all", "Tout effacer"),

            // Dictionary
            ["dictionary.title"] = ("Dictionary", "Dictionnaire"),
            ["dictionary.subtitle"] = (
                "Manage the personal corrections applied to transcription.",
                "Gérez les corrections personnelles appliquées à la transcription."),
            ["dictionary.add.title"] = ("Add a correction", "Ajouter une correction"),
            ["dictionary.add.desc"] = (
                "The recognized form will be replaced by the given text during rewriting.",
                "La forme reconnue sera remplacée par le texte indiqué lors de la réécriture."),
            ["dictionary.spokenForm"] = ("RECOGNIZED FORM", "FORME RECONNUE"),
            ["dictionary.replacement"] = ("REPLACEMENT", "REMPLACEMENT"),
            ["dictionary.addUpdate"] = ("Add / update", "Ajouter / mettre à jour"),
            ["dictionary.local.title"] = ("Local corrections", "Corrections locales"),
            ["dictionary.search"] = ("SEARCH", "RECHERCHER"),
            ["dictionary.exchange.title"] = ("Import / Export", "Importer / Exporter"),
            ["dictionary.exchange.desc"] = (
                "Back up your corrections to a local file, or import one. Imported content is validated and overwrites nothing without your choice. No data is sent outside.",
                "Sauvegardez vos corrections dans un fichier local, ou importez-en un. Le contenu importé est validé et n'écrase rien sans votre choix. Aucune donnée n'est envoyée à l'extérieur."),
            ["dictionary.export"] = ("Export…", "Exporter…"),
            ["dictionary.import"] = ("Import…", "Importer…"),
            ["dictionary.overwriteConflicts"] = (
                "Overwrite existing entries on conflict",
                "Écraser les entrées existantes en cas de conflit"),
            ["common.edit"] = ("Edit", "Modifier"),
            ["common.delete"] = ("Delete", "Supprimer"),
            ["common.cancel"] = ("Cancel", "Annuler"),

            // Profiles
            ["profiles.title"] = ("Profiles", "Profils"),
            ["profiles.subtitle"] = (
                "Choose the local transformation applied after the dictionary, before insertion.",
                "Choisissez la transformation locale appliquée après le dictionnaire, avant l'insertion."),
            ["profiles.sessionBadge"] = ("SESSION · NOT SAVED", "SESSION · NON ENREGISTRÉ"),
            ["profiles.info"] = (
                "The selection applies to this session only and is never stored. The profile used by a dictation is the one selected when recording starts; a change during recording applies to the next dictation. No profile invents content.",
                "La sélection vaut pour cette session uniquement et n'est jamais enregistrée. Le profil utilisé par une dictée est celui choisi au démarrage de l'enregistrement ; un changement pendant l'enregistrement s'applique à la dictée suivante. Aucun profil n'invente de contenu."),
            ["profiles.auth.title"] = ("Sign-in", "Connexion"),
            ["profiles.auth.signInGoogle"] = ("Sign in with Google", "Se connecter avec Google"),
            ["profiles.auth.signOut"] = ("Sign out", "Se déconnecter"),
            ["profiles.auth.signingIn"] = ("Signing in…", "Connexion en cours…"),
            ["profiles.auth.connected"] = ("Connected account", "Compte connecté"),
            ["profiles.cloud.title"] = ("Cloud rewriting", "Réécriture Cloud"),

            // Profiles — auth badges (code-behind)
            ["profiles.auth.badge.connected"] = ("CONNECTED · GOOGLE", "CONNECTÉ · GOOGLE"),
            ["profiles.auth.badge.signingIn"] = ("SIGNING IN…", "CONNEXION…"),
            ["profiles.auth.badge.offline"] = ("SERVICE UNAVAILABLE", "SERVICE INDISPONIBLE"),
            ["profiles.auth.badge.expired"] = ("SESSION EXPIRED", "SESSION EXPIRÉE"),
            ["profiles.auth.badge.failed"] = ("CONNECTION FAILED", "ÉCHEC DE CONNEXION"),
            ["profiles.auth.badge.unconfigured"] = ("NOT CONFIGURED", "NON CONFIGURÉ"),
            ["profiles.auth.badge.local"] = ("LOCAL MODE", "MODE LOCAL"),

            // Profiles — cloud (code-behind)
            ["profiles.cloud.badge.local"] = ("LOCAL MODE", "MODE LOCAL"),
            ["profiles.cloud.badge.unavailable"] = ("SERVICE UNAVAILABLE", "SERVICE INDISPONIBLE"),
            ["profiles.cloud.badge.active"] = ("CLOUD · {0} ACTIVE", "CLOUD · {0} ACTIF"),
            ["profiles.cloud.toggle.enable"] = ("Enable Cloud", "Activer le Cloud"),
            ["profiles.cloud.toggle.disable"] = ("Disable Cloud", "Désactiver le Cloud"),
            ["profiles.cloud.provider.active"] = ("{0} · active", "{0} · actif"),
            ["profiles.cloud.status.unauthenticated.unconfigured"] = (
                "Local mode is active. Configure authentication before any Cloud option.",
                "Le mode local est utilisé. Configurez l'authentification avant toute option Cloud."),
            ["profiles.cloud.status.unauthenticated.expired"] = (
                "Local mode is active. A valid connection is required before any Cloud option.",
                "Le mode local est utilisé. Une connexion valide est requise avant toute option Cloud."),
            ["profiles.cloud.consent.unauthenticated"] = (
                "Sign-in required. No text is sent to the Cloud until you sign in.",
                "Connexion requise. Tant que vous n'êtes pas connecté, aucun texte ne part vers le Cloud."),
            ["profiles.cloud.status.backendUnavailable.enabled"] = (
                "Cloud enabled for this session with {0}, but {1} Local mode is active.",
                "Cloud activé pour cette session avec {0}, mais {1} Le mode local est utilisé."),
            ["profiles.cloud.status.backendUnavailable.disabled"] = (
                "Local mode is active. {0}",
                "Le mode local est utilisé. {0}"),
            ["profiles.cloud.consent.backendUnavailable.granted"] = (
                "Consent granted for this session. No text is sent while the backend is unavailable.",
                "Consentement accordé pour cette session. Aucun texte ne part tant que le backend est indisponible."),
            ["profiles.cloud.consent.backendUnavailable.notGranted"] = (
                "First Cloud use requires your explicit consent. Audio always stays local.",
                "Le premier usage Cloud demande votre consentement explicite. L'audio reste toujours local."),
            ["profiles.cloud.status.enabled"] = (
                "Cloud enabled: transcribed text is rewritten by {0}, with exact local fallback on failure.",
                "Cloud activé : le texte transcrit est reformulé par {0}, avec repli local exact en cas d'échec."),
            ["profiles.cloud.status.disabled"] = (
                "Local mode is active. No data is sent over the Internet.",
                "Le mode local est utilisé. Aucune donnée n'est envoyée sur Internet."),
            ["profiles.cloud.consent.granted"] = (
                "Consent granted for this session. Audio always stays local.",
                "Consentement accordé pour cette session. L'audio reste toujours local."),
            ["profiles.cloud.consent.notGranted"] = (
                "First Cloud use requires your explicit consent. Audio always stays local.",
                "Le premier usage Cloud demande votre consentement explicite. L'audio reste toujours local."),
            ["profiles.cloud.backendNotConfigured"] = (
                "The Cloud backend is not configured.",
                "Le backend Cloud n'est pas configuré."),

            // Profiles — consent dialog (code-behind)
            ["profiles.cloud.consent.message"] = (
                "Transcribed text will be sent to the Cloud service for rewriting.\n\nAudio always stays local and is never sent.\n\nDo you want to enable Cloud rewriting?",
                "Le texte transcrit sera envoyé au service Cloud afin d'être reformulé.\n\nL'audio reste toujours local et n'est jamais envoyé.\n\nSouhaitez-vous activer la réécriture Cloud ?"),
            ["profiles.cloud.consent.caption"] = (
                "Cloud rewriting consent",
                "Consentement à la réécriture Cloud"),

            // Profiles — profile cards (code-behind)
            ["profiles.card.badge.unavailable"] = ("UNAVAILABLE", "INDISPONIBLE"),
            ["profiles.card.action.unavailable"] = ("Unavailable", "Indisponible"),
            ["profiles.card.badge.active"] = ("ACTIVE · SAVED", "ACTIF · ENREGISTRÉ"),
            ["profiles.card.action.active"] = ("Active profile", "Profil actif"),
            ["profiles.card.badge.available"] = ("AVAILABLE", "DISPONIBLE"),
            ["profiles.card.action.use"] = ("Use this profile", "Utiliser ce profil"),

            // Subscription
            ["subscription.title"] = ("Subscription", "Abonnement"),
            ["subscription.subtitle"] = (
                "Your plan and options. Fluent stays 100% local and free; Pro will add advanced options.",
                "Votre plan et vos options. Fluent reste 100 % local et gratuit ; Pro ajoutera des options avancées."),
            ["subscription.currentPlan"] = ("CURRENT PLAN", "PLAN ACTUEL"),
            ["subscription.plan.local"] = ("Local · Free", "Local · Gratuit"),
            ["subscription.plan.desc"] = (
                "Local dictation, rewriting, dictionary and history, no account required.",
                "Dictée, réécriture, dictionnaire et historique locaux, sans compte requis."),
            ["subscription.upgrade"] = ("Upgrade to Pro", "Passer à Pro"),
            ["subscription.account"] = ("Account", "Compte"),
            ["subscription.account.local"] = ("Not signed in · local use", "Non connecté · utilisation locale"),
            ["subscription.account.connected"] = ("Signed in · ", "Connecté · "),
            ["subscription.account.connectedNoEmail"] = ("Signed in", "Compte connecté"),
            ["subscription.upgrade.status"] = (
                "Pro is not available yet in this local version. No payment data is requested.",
                "L'abonnement Pro n'est pas encore disponible dans cette version locale. Aucune donnée de paiement n'est demandée."),
            ["subscription.pro.title"] = ("Pro · coming soon", "Pro · à venir"),
            ["subscription.pro.item1"] = (
                "• Extended transcription models and advanced rewriting profiles.",
                "• Modèles de transcription étendus et profils de réécriture avancés."),
            ["subscription.pro.item2"] = (
                "• Extended dictionary/glossary and optional encrypted sync.",
                "• Dictionnaire/glossaire étendu et synchronisation optionnelle chiffrée."),
            ["subscription.pro.item3"] = (
                "• Enriched history and advanced exports.",
                "• Historique enrichi et exports avancés."),
            ["subscription.pro.note"] = (
                "The local mode will always stay available and free. No payment is requested in this version.",
                "Le mode local restera toujours disponible et gratuit. Aucun paiement n'est demandé dans cette version."),

            // Settings
            ["settings.title"] = ("Settings", "Paramètres"),
            ["settings.subtitle"] = (
                "Local, versioned and reversible preferences. No secret or Cloud consent is stored here.",
                "Préférences locales, versionnées et réversibles. Aucun secret ni consentement Cloud n'est enregistré ici."),
            ["settings.defaultProfile"] = ("Default profile", "Profil par défaut"),
            ["settings.defaultProfile.current"] = ("Current: ", "Actuel : "),
            ["settings.defaultProfile.desc"] = (
                "The profile applied to rewriting at startup. This choice is saved locally and restored on the next launch.",
                "Le profil appliqué à la réécriture au démarrage. Ce choix est enregistré localement et restauré au prochain lancement."),
            ["settings.storage.title"] = ("Local storage and privacy", "Stockage local et confidentialité"),
            ["settings.storage.desc1"] = (
                "All data stays on this device, in the local application folder (LocalAppData\\Fluent): dictionary, history and preferences, each in its own SQLite database.",
                "Toutes les données restent sur cet appareil, dans le dossier local de l'application (LocalAppData\\Fluent) : dictionnaire, historique et préférences, chacun dans sa propre base SQLite."),
            ["settings.storage.desc2"] = (
                "No audio file is ever stored. No telemetry. Authentication and Cloud options are session-only and are not persisted.",
                "Aucun fichier audio n'est jamais enregistré. Aucune télémétrie. L'authentification et les options Cloud restent limitées à la session et ne sont pas persistées."),
            ["settings.session.title"] = ("Session and engine", "Session et moteur"),
            ["settings.session.desc"] = (
                "Current session information (previously shown in the header).",
                "Informations de la session en cours (auparavant affichées dans l'en-tête)."),
            ["settings.session.profile"] = ("PROFILE", "PROFIL"),
            ["settings.session.mode"] = ("MODE", "MODE"),
            ["settings.session.engine"] = ("ENGINE", "MOTEUR"),
            ["settings.transcriptionLanguage.title"] = ("Transcription language", "Langue de transcription"),
            ["settings.transcriptionLanguage.desc"] = (
                "Fluent automatically detects whether you are speaking French or English. You can force a language when needed.",
                "Fluent détecte automatiquement si vous parlez français ou anglais. Vous pouvez imposer une langue en cas de besoin."),
            ["settings.language.title"] = ("Language", "Langage"),
            ["settings.language.desc"] = (
                "Interface language (default: English).",
                "Langue de l'interface (par défaut : anglais)."),
            ["settings.history.manage"] = ("Manage history", "Gérer l'historique"),
            ["settings.history.prefix"] = ("History · ", "Historique · "),

            // Dictation flow — status bar states (MainWindow code-behind)
            ["dictation.status.active"] = ("Active", "Actif"),
            ["dictation.status.unavailable"] = ("Unavailable", "Indisponible"),
            ["dictation.status.ready"] = ("Ready", "Prêt"),
            ["dictation.status.failed"] = ("Failed", "Échec"),
            ["dictation.status.blocked"] = ("Blocked", "Bloqué"),
            ["dictation.status.recording"] = ("Recording", "Enregistrement"),
            ["dictation.status.stoppingMic"] = ("Stopping microphone", "Arrêt du microphone"),
            ["dictation.status.noSpeech"] = ("No speech", "Pas de parole"),
            ["dictation.status.rewriting"] = ("Rewriting", "Réécriture"),
            ["dictation.status.inserted"] = ("Inserted", "Inséré"),
            ["dictation.status.clipboardFallback"] = ("Clipboard fallback", "Presse-papiers"),
            ["dictation.status.preparing"] = ("Preparing", "Préparation"),
            ["dictation.status.downloading"] = ("Downloading", "Téléchargement"),
            ["dictation.status.loading"] = ("Loading", "Chargement"),
            ["dictation.status.optimizing"] = ("Optimizing", "Optimisation"),
            ["dictation.status.transcribing"] = ("Transcribing", "Transcription"),

            // Dictation flow — busy guard
            ["dictation.busy"] = (
                "Fluent is already processing the current dictation.",
                "Fluent traite déjà la dictée en cours."),

            // Dictation flow — StartRecording guards
            ["dictation.guard.noTarget"] = (
                "No active target could be captured.",
                "Aucune cible active n'a pu être capturée."),
            ["dictation.guard.passwordBlocked"] = (
                "Password target blocked. Nothing was recorded, pasted, or copied.",
                "Cible de type mot de passe bloquée. Rien n'a été enregistré, collé ou copié."),
            ["dictation.guard.unverifiable"] = (
                "Target safety or focused-field identity could not be verified. Nothing was recorded, pasted, or copied.",
                "La sécurité de la cible ou l'identité du champ focalisé n'a pas pu être vérifiée. Rien n'a été enregistré, collé ou copié."),

            // Dictation flow — recording started
            ["dictation.recording.started"] = (
                "Recording in memory. Press Ctrl+Space again to transcribe and insert.",
                "Enregistrement en mémoire. Appuyez sur Ctrl+Espace à nouveau pour transcrire et insérer."),

            // Dictation flow — Push-to-Talk states
            ["dictation.status.pushToTalk.arming"] = (
                "Hold…",
                "Maintenez…"),
            ["dictation.recording.pushToTalk"] = (
                "Recording… Release Ctrl+Win to finish.",
                "Enregistrement… Relâchez Ctrl+Win pour terminer."),
            ["dictation.pushToTalk.cancelled"] = (
                "Hold too short. Nothing was recorded.",
                "Maintien trop court. Rien n'a été enregistré."),
            ["dictation.pushToTalk.maxDuration"] = (
                "Maximum recording duration reached. Processing…",
                "Durée maximale d'enregistrement atteinte. Traitement…"),

            // Dictation flow — audio preparation
            ["dictation.audio.preparing"] = ("Preparing audio…", "Préparation audio…"),

            // Dictation flow — too short / no speech
            ["dictation.tooShort"] = (
                "Recording was too short. Nothing was pasted or copied.",
                "L'enregistrement était trop court. Rien n'a été collé ou copié."),
            ["dictation.noSpeechRecognized"] = (
                "No speech was recognized. Nothing was pasted or copied.",
                "Aucune parole n'a été reconnue. Rien n'a été collé ou copié."),

            // Dictation flow — rewriting phase
            ["dictation.rewriting.profileApplied.zero"] = (
                "Local application of the {0} profile.",
                "Application locale du profil {0}."),
            ["dictation.rewriting.profileApplied.one"] = (
                "1 dictionary correction applied before the {0} profile.",
                "1 correction du dictionnaire appliquée avant le profil {0}."),
            ["dictation.rewriting.profileApplied.many"] = (
                "{0} dictionary corrections applied before the {1} profile.",
                "{0} corrections du dictionnaire appliquées avant le profil {1}."),
            ["dictation.rewriting.capsule"] = ("Rewriting…", "Réécriture…"),

            // Capsule accessibility
            ["capsule.cancel.name"] = ("Cancel dictation", "Annuler la dictée"),
            ["capsule.paste.name"] = ("Paste transcription", "Coller la transcription"),

            // Dictation flow — InsertTranscript status labels
            ["dictation.insert.profileVia"] = (
                "Profile {0} applied via {1}.",
                "Profil {0} appliqué via {1}."),
            ["dictation.insert.cloudUnavailable"] = (
                "Cloud service unavailable ({0}): exact local text preserved.",
                "Service Cloud indisponible ({0}) : texte local exact conservé."),
            ["dictation.insert.profileLocal"] = (
                "Profile {0} applied locally.",
                "Profil {0} appliqué localement."),

            // Dictation flow — dictionary scope
            ["dictation.scope.local"] = ("local dictionary", "dictionnaire local"),
            ["dictation.scope.session"] = ("session-only dictionary", "dictionnaire de session"),

            // Dictation flow — dictionary status
            ["dictation.dictionary.one"] = (
                "1 {0} correction applied.",
                "1 correction du {0} appliquée."),
            ["dictation.dictionary.many"] = (
                "{0} {1} corrections applied.",
                "{0} corrections du {1} appliquées."),

            // Dictation flow — paste results
            ["dictation.paste.success"] = (
                "French speech transcribed locally and inserted into the locked target.",
                "Parole française transcrite localement et insérée dans la cible verrouillée."),
            ["dictation.paste.keyboardFailed"] = (
                "Paste shortcut could not be sent ({0}/{1} inputs, Windows error {2}). The transcript remains on the clipboard.",
                "Le raccourci de collage n'a pas pu être envoyé ({0}/{1} entrées, erreur Windows {2}). Le texte reste dans le presse-papiers."),
            ["dictation.paste.targetChanged"] = (
                "Target changed during dictation. Transcript copied to clipboard, not pasted.",
                "La cible a changé pendant la dictée. Texte copié dans le presse-papiers, non collé."),
            ["dictation.paste.passwordBlocked"] = (
                "Password target blocked. Transcript was not pasted or copied.",
                "Cible de type mot de passe bloquée. Le texte n'a été ni collé ni copié."),
            ["dictation.paste.unverified"] = (
                "Target safety could not be verified. Transcript was not pasted or copied.",
                "La sécurité de la cible n'a pas pu être vérifiée. Le texte n'a été ni collé ni copié."),
            ["dictation.paste.missing"] = (
                "Target missing. Transcript was not pasted or copied.",
                "Cible absente. Le texte n'a été ni collé ni copié."),
            ["dictation.paste.noTarget"] = ("No current target", "Aucune cible actuelle"),

            // Dictation flow — timing
            ["dictation.timing"] = (
                "Stop-to-text: {0:F1}s for {1:F1}s of audio.",
                "Arrêt→texte : {0:F1}s pour {1:F1}s d'audio."),

            // Dictation flow — model download messages
            ["dictation.model.downloading"] = ("Model ~80 MB…", "Modèle ~80 Mo…"),
            ["dictation.model.firstUse"] = (
                "First use only: downloading the multilingual Whisper model. Audio remains local.",
                "Premier usage uniquement : téléchargement du modèle Whisper multilingue. L'audio reste local."),
            ["dictation.model.processing"] = (
                "Processing locally. Audio is held in memory only.",
                "Traitement local. L'audio est conservé en mémoire uniquement."),
            ["dictation.model.ready"] = (
                "Recording in memory. The local transcription model is ready; release Ctrl+Win to finish.",
                "Enregistrement en mémoire. Le modèle de transcription local est prêt ; relâchez Ctrl+Win pour terminer."),
            ["dictation.model.retryAfterStop"] = (
                "Recording continues. Model preparation will retry after stop: {0}",
                "L'enregistrement continue. La préparation du modèle réessaiera après l'arrêt : {0}"),

            // Dictation flow — recording preparation stages
            ["dictation.prep.firstUseDownload"] = (
                "First use: downloading the local model during recording. Audio stays local.",
                "Premier usage : téléchargement du modèle local pendant l'enregistrement. L'audio reste local."),
            ["dictation.prep.background"] = (
                "Recording in progress. Local engine preparing in background.",
                "Enregistrement en cours. Préparation locale du moteur en arrière-plan."),

            // Dictation flow — describe target
            ["dictation.target.unknown"] = ("unknown focused element", "élément focalisé inconnu"),

            // Dictation flow — user identity fallback
            ["dictation.user.fallback"] = ("User", "Utilisateur"),

            // Dictation flow — history nav status
            ["dictation.history.off"] = ("OFF", "DÉSACTIVÉ"),

            // Dictation flow — profile / engine labels
            ["dictation.profile.local"] = ("Local", "Local"),
            ["dictation.profile.cloudWith"] = ("Cloud · {0}", "Cloud · {0}"),
            ["dictation.engine.label"] = ("Base Q8 · CPU", "Base Q8 · CPU"),
            ["dictation.profile.prefix"] = ("Profile · ", "Profil · "),

            // Dictation flow — dictionary nav status
            ["dictation.nav.local"] = ("LOCAL", "LOCAL"),
            ["dictation.nav.session"] = ("SESSION", "SESSION"),
            ["dictation.nav.loading"] = ("LOADING", "CHARGEMENT"),
        };
}
