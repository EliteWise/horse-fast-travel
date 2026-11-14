# Recette de validation en jeu

## État de validation

Effectué : inspection des DLL locales, compilation Release contre Stardew Valley
1.6.15 / SMAPI 4.3.2 et tests automatisés sans interface. **Les scénarios ci-dessous
n'ont pas été exécutés en jeu dans cet environnement.** SMAPI 4.5.2, le rendu,
les sauvegardes et les connexions multijoueur restent à valider.

Utiliser une sauvegarde de test sauvegardée au préalable. Installer la même
version du mod chez l'hôte et les invités. Garder le journal SMAPI visible.

## Scénarios

| Situation / action | Résultat attendu |
| --- | --- |
| Sans écurie, écurie en construction, cheval non nommé ou appartenant à un autre joueur | Aucun menu de voyage. Actions natives conservées. |
| Écurie personnelle terminée, cheval libre, joueur à proximité | Dialogue natif à un seul choix textuel « Aller au magasin de Pierre (1000 po) » et une croix ; aucune seconde confirmation ni bouton encadré personnalisé. |
| Modifier `TravelCost` puis ouvrir le menu | Le prix affiché entre parenthèses correspond exactement au montant prélevé. |
| Cliquer directement sur le cheval, avec ou sans chapeau/carotte | Action native habituelle ; aucun menu de voyage ni prélèvement. |
| Porter ces objets puis choisir le voyage | Montage automatique ; aucun objet consommé ou équipé par le mod. |
| 999 po puis tentative avec la configuration par défaut | Refus, solde inchangé, cheval inchangé. |
| Exactement 1 000 po puis voyage réussi | Solde 0, une seule instance du même cheval, joueur toujours monté devant Pierre. |
| Fermer par la croix, Échap ou B sans sélectionner le voyage | Aucun prélèvement, aucun départ, contrôles rétablis ; Échap ne valide jamais le voyage. |
| Redimensionner la fenêtre puis utiliser souris ou manette | Bouton et croix restent visibles, cliquables et navigables. |
| Observer la scène et maintenir mouvement, outil, action et menu, au clavier puis manette | Déplacement visible du cavalier et du cheval, sabots animés et sons natifs, aucun contrôle manuel. |
| Durées 500, 2 000 et 10 000 ms | Course de la durée configurée avant le fondu ; aucune heure ajoutée. |
| Échap/B pendant la course, puis appui répété | Retour au point de départ, remboursement une seule fois, contrôles rétablis. |
| Fermer la fenêtre ou suspendre l'hôte pendant la scène | Arrêt sûr ou expiration du trajet, remboursement, aucun blocage au retour. |
| Bloquer le départ avec clôtures/arbres/objets | Trajet accepté ; obstacles ignorés pendant la cinématique, sans détruire ou déplacer les objets. |
| Lancer le voyage depuis chaque côté de l'écurie, puis après l'avoir déplacée | Cavalier et cheval sont placés devant la façade sud avant la course ; aucun départ sous le toit ou derrière l'écurie. |
| Annuler après le placement devant l'écurie, y compris si cet endroit est encombré | Retour à la position du joueur avant le voyage, toujours monté, remboursement unique et contrôles rétablis. |
| Départ près de chaque bord de la carte | Cinématique visible de deux secondes qui reste sur la carte. |
| Un PNJ ou un autre joueur se place sur la course ou l'arrivée | Le voyage continue ; sa présence ne fait pas éviter la case d'arrivée. |
| Bloquer toutes les cases d'arrivée avec des obstacles statiques | Voyage refusé avec le message « Quelque chose bloque l'arrivée ! », aucun prélèvement et contrôles conservés. |
| Arriver avant ouverture, après fermeture ou le mercredi | Arrivée dehors ; porte et horaires inchangés. |
| Interagir avec le cheval / Pierre en ville | Actions natives uniquement, aucun menu ni trajet vers la ferme. |
| Jour de festival | Refus sans paiement ni lancement de fête ou écran d'attente. |
| Un événement commence avant/pendant/après le trajet | Le mod ne pilote plus le joueur, ne déplace aucun acteur de l'événement et rembourse. |
| Descendre, remonter, dormir, sauvegarder, recharger | Cheval unique, même identifiant et propriétaire, écurie fonctionnelle le lendemain. |
| Quitter vers le titre pendant une attente réseau ou scène | Aucun état de trajet repris à la prochaine sauvegarde. |
| Deux joueurs voyagent simultanément avec leurs propres chevaux | Positions, solde et contrôles propres à chaque joueur ; aucun emprunt de cheval tiers. |
| Invité, cheval présent sur une autre carte | L'hôte transfère cette instance ; le client attend la synchronisation puis réserve et monte. |
| Même scénario sans mod chez l'hôte | Refus explicite, aucun paiement ni montage partiel. |
| Réponse de verrou tardive après annulation ou déconnexion | Aucun départ ni prélèvement tardif ; aucun verrou inutilisé conservé. |
| Deux joueurs en écran partagé, un seul voyage | Seul son écran est gelé et transféré ; l'autre reste jouable. |
| Configuration négative/excessive, prix nul, retour d'une ancienne configuration avec `EnableReturnTrip` | Valeurs bornées ; aucun prix négatif ; trajet gratuit accepté ; ancienne option ignorée et aucun retour proposé. |

Pour contrôler les duplications, vérifier la présence du cheval après la descente,
sur les deux clients, puis après sauvegarde et rechargement. Un cheval monté est
normalement absent de `location.characters` : compter uniquement cette collection
pendant le voyage donnerait un faux résultat.
