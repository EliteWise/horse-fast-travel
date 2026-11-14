# API vérifiées

Inspection directe des DLL installées de Stardew Valley 1.6.15 et SMAPI 4.3.2,
puis compilation C# contre ces mêmes DLL. Version publiée vérifiée le 11 novembre 2026 :
[SMAPI 4.5.2](https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2), pour Stardew Valley 1.6.14+.
La compatibilité avec SMAPI 4.5.2 est visée ; son exécution n'a pas été testée ici.

| API | Comportement vérifié et utilisation |
| --- | --- |
| `Farmer.isRidingHorse()` | Renvoie faux pendant `Game1.eventUp`. Un événement narratif ferait perdre le rendu monté normal. |
| `PathFindController.update(GameTime)` | Méthode virtuelle appelée par `Farmer.Update`. Le contrôleur dérivé déplace le vrai cavalier, sans lancer d'événement ni remplacer les assets. |
| `Horse.update`, `Farmer.showRiding()` | Sélectionnent les frames natives, le mouvement du cavalier et les sons de pas. Le mod conserve `CanMove` pendant la scène pour que ces animations fonctionnent. |
| `Game1.freezeControls` | Empêche `UpdateControlInput`, qui appellerait autrement `Farmer.Halt()` et réinitialiserait l'animation du cheval à chaque frame. N'empêche pas `UpdateCharacters`. Restauré avant le warp ou à l'annulation ; les événements arrivant entre-temps gardent leurs contrôles. |
| `Horse.rider`, `Farmer.mount`, `characters.Remove` | Relations utilisées à la fin du montage natif. Le cheval monté ne doit plus figurer dans la collection des PNJ. Le mod réutilise cette instance ; il ne construit jamais de cheval. |
| `Horse.checkAction` | Peut nommer un cheval, poser un chapeau ou donner une carotte. Conservé pour l'action normale ; le trajet n'appelle pas cette méthode pour monter automatiquement. |
| `Horse.mutex.RequestLock`, `IsLockHeld`, `ReleaseLock` | Réserve la monture avant tout prélèvement. Un cavalier conserve le verrou jusqu'à sa descente native. Une réponse tardive après annulation libère le verrou sans démarrer le trajet. |
| `Stable.HorseId`, `owner`, `Horse.ownerId` | Vérifie l'association écurie/cheval/joueur. Un autre joueur ne peut pas emprunter cette monture par le mod. |
| `Stable.GetDefaultHorseTile()`, `tileY`, `tilesHigh` | Aligne le départ sur l'emplacement du cheval, sur la rangée au sud de l'emprise de l'écurie. Cavalier et cheval y sont placés avant la course, sans vérifier les obstacles, en respectant les limites de la carte. |
| `Game1.warpCharacter` | Côté invité, n'est pas immédiat. Le mod demande à l'hôte installé de transférer le cheval du demandeur, attend la synchronisation native, puis réserve la monture sur la même carte que le joueur. |
| `Game1.warpFarmer(..., int facingDirectionAfterWarp)` | Déclenche le fondu natif, le chargement de la carte et la synchronisation multijoueur. Un transfert extérieur → extérieur conserve la monture. |
| `GameLocation.getWarpPointTo("SeedShop", who)` | Trouve la porte de Pierre dans les portes/warps chargés, sans coordonnées codées en dur. Le point d'arrivée est cherché à proximité, dehors. |
| `isCollidingPosition(..., pathfinding: true, skipCollisionEffects: true)` | Vérifie les collisions statiques sans pousser de PNJ. Les PNJ et les autres joueurs ne bloquent pas l'arrivée. Le mod vérifie l'emprise complète du cheval, les limites, les portes et les `TouchAction`. |
| `PerScreen<T>` et événements SMAPI | État du trajet distinct pour chaque écran local. Les champs statiques de contexte du jeu sont gérés par son système natif d'écran partagé. |
| `DialogueBox.receiveKeyPress` | Pour une question, Échap/N sélectionne la dernière réponse. Le dialogue dérivé intercepte ces touches et B pour fermer sans valider l'unique réponse de voyage. |
| `DialogueBox` | Le voyage conserve le rendu, le texte à choix, le survol, la navigation et les transitions du dialogue natif. Seule une croix de fermeture native est ajoutée ; aucun cadre de bouton personnalisé n'est dessiné. |

Le mod ne modifie jamais `Game1.timeOfDay`, ne remplace pas d'événement, ne force pas
l'ouverture de Pierre et n'écrit aucun fichier du jeu. Le gel natif suspend
normalement l'horloge en solo pendant la scène ; en multijoueur l'horloge du monde
reste gouvernée par l'hôte. La durée utilise les millisecondes de `GameTime`, avec
un plafond par frame pour que les ralentissements ne sautent pas toute l'animation.

Limites assumées de cette première version : trajet refusé les jours de festival
ou sur une carte dont la porte de Pierre est introuvable ou l'arrivée encombrée.
Le cheval rejoint le joueur, puis tous deux sont placés devant l'écurie avant la course.
Le mouvement de cette
cinématique ignore les collisions du départ, tout en restant dans les limites de
la carte. Aucun drapeau global de collision n'est modifié ; la vérification de
l'arrivée reste active. Une annulation avant le fondu replace le joueur à sa position
initiale, avant son placement devant l'écurie.
Un événement natif déclenché à l'arrivée garde la priorité : le mod rembourse et
cesse de piloter le joueur. Il ne désactive pas cet événement.

La compilation et les tests sans interface ne prouvent pas le rendu, les transitions
réseau ou le cycle sauvegarde/chargement. Voir la recette de validation en jeu.
