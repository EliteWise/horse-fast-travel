# Horse Fast Travel

Mod C# pour Stardew Valley : un trajet **uniquement de la ferme vers Pierre**, sur
le vrai cheval du joueur, avec une course visible de deux secondes suivie du fondu
natif. Prix par défaut : **1 000 po**. Aucun trajet retour n'est proposé.

## Utilisation

1. Construire une écurie et donner un nom au cheval pour en devenir propriétaire.
2. Sur la ferme, interagir avec sa propre écurie à portée normale.
3. Choisir **« Aller au magasin de Pierre (1000 po) »** : le prix configuré est
   affiché directement et le voyage démarre sans seconde confirmation.
4. Le joueur monte sur son cheval ; tous deux sont placés devant l'écurie avant
   la course automatique, puis arrivent ensemble devant Pierre, toujours montés.

La croix du menu, Échap ou B sur la manette ferme la fenêtre sans paiement ni
voyage. Le clic direct sur le cheval conserve son interaction habituelle ; cliquer
sur le bâtiment permet de voyager. Hors de la ferme,
le mod n'intercepte aucune interaction de voyage. Pierre conserve ses horaires et
sa porte fonctionne normalement. Les textes sont disponibles en français et anglais.

Pendant la course, Échap ou B sur la manette annule le trajet et rembourse le prix.
Après le début du fondu, le chargement natif peut déjà être engagé : l'annulation
ne force pas son arrêt, et les frais sont remboursés.

## Installation

Stardew Valley **1.6.14+** et SMAPI **4.3.2+** sont requis. Les API ont été vérifiées
et le mod compilé avec Stardew Valley **1.6.15** et SMAPI **4.3.2** installés ici.
La version actuelle publiée de [SMAPI est 4.5.2](https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2).

Extraire `HorseFastTravel 1.0.0.zip` dans `Mods`, afin d'obtenir
`Mods/HorseFastTravel/manifest.json`. En multijoueur, installer le mod chez l'hôte et
les joueurs qui souhaitent voyager. Un invité dont le cheval est sur une autre
carte demande à l'hôte de déplacer cette instance avant de réserver la monture.

## Configuration

`config.json` est créé/lu au lancement :

```json
{
  "TravelCost": 1000,
  "AnimationDurationMs": 2000
}
```

`TravelCost` accepte 0 à 1 000 000 ; `AnimationDurationMs` accepte 500 à 10 000 ms.
Les valeurs invalides sont bornées avec un avertissement SMAPI. Redémarrer le jeu
après modification. Le prix n'est prélevé qu'après les vérifications et la
réservation du cheval. Une interruption rembourse exactement le prix prélevé.
Le mod n'ajoute aucune heure au temps de jeu.

## Compilation et vérification

Avec le SDK .NET 6 ou plus récent et une installation locale du jeu avec SMAPI :

```sh
dotnet build ModName.csproj -c Release
```

Si la détection automatique du jeu échoue, ajouter `-p:GamePath="chemin du jeu"`.
Le ZIP est généré dans `bin/Release/net6.0/`. La compilation déploie aussi le mod
dans `Mods/HorseFastTravel` de l'installation détectée. Pour compiler sans installer,
ajouter `-p:EnableModDeploy=false`.

Les tests sans interface nécessitent le SDK .NET 8 et la DLL MonoGame du jeu :

```sh
dotnet run --project tests/HorseFastTravel.Tests.csproj -p:GamePath="chemin du jeu"
```

Ils couvrent les frais, les remboursements, la progression de la cinématique,
les limites de la carte et la configuration. La validation visuelle et réseau
reste à effectuer en jeu : voir [la recette](docs/TESTING.md) et les
[API inspectées](docs/API-NOTES.md).

## Limites de cette version

- Les obstacles du départ sont ignorés pendant la cinématique uniquement : le
  cheval peut traverser un objet ou une clôture pendant ces deux secondes.
  La course commence devant l'écurie, sans vérifier les obstacles à cet endroit.
  Les limites de la carte restent respectées. L'arrivée doit rester praticable.
- Trajets indisponibles les jours de festival pour éviter les cartes de fête et
  les vérifications de présence multijoueur.
- Une écurie en construction, sans cheval possédé, ou un cheval occupé ne permet
  aucun voyage. Aucun cheval de remplacement n'est créé.
- Les événements natifs gardent la priorité, y compris ceux déclenchés en arrivant
  en ville. Le mod interrompt alors son pilotage et rembourse.
- La scène utilise le gel de contrôles natif : l'horloge peut rester en pause en
  solo ; en multijoueur, le temps du monde reste celui de l'hôte.
- Après une annulation pendant la course, le joueur revient au point de départ,
  sur son cheval. En cas d'arrivée devenue entièrement bloquée entre la
  vérification et le chargement, un transfert de récupération vers le départ est
  demandé ; ce n'est pas une option de voyage accessible au joueur.

Aucune ressource graphique personnalisée, patch Harmony ou dépendance à un autre
mod n'est nécessaire.
