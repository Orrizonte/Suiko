# Suikoden I HD – Trois runes selon la MAG

Mod BepInEx (IL2CPP) pour **Suikoden I & II HD Remaster** (Steam) : jusqu'à 3 emplacements de rune par personnage dans Suikoden I, débloqués selon la MAG, comme dans Suikoden II.

- `SuikodenThreeRunes` : les 3 emplacements, le menu Runes en combat, la boutique de runes, le menu principal et la sauvegarde.
- `SuikodenThreeRunesPassives` (addon, facultatif) : les runes passives (Fortune, Double-Beat, Tueur, Rafale…) équipées dans les emplacements 2/3 agissent aussi.

## Installation

1. BepInEx 6 (IL2CPP) doit déjà être installé et le jeu lancé une fois (dossier `BepInEx/interop` généré).
2. Copier le contenu de `release/` dans le dossier du jeu :
   - `BepInEx/plugins/SuikodenThreeRunes/SuikodenThreeRunes.dll`
   - `BepInEx/plugins/SuikodenThreeRunesPassives/SuikodenThreeRunesPassives.dll`
3. Supprimer les anciennes versions de ces deux DLL si elles sont ailleurs dans `BepInEx/plugins`.

Les sauvegardes faites avec les versions précédentes restent compatibles.

Le mod vérifie l'empreinte SHA-256 de `GameAssembly.dll`. Après une mise à jour du jeu, il se désactive tout seul (message dans `BepInEx/LogOutput.log`).

## Configuration

`BepInEx/config/fr.suikodenhd.threerunes.cfg` :

| Clé | Défaut | Rôle |
|---|---|---|
| `MAG_Deuxieme_Rune` | 60 | MAG minimale pour le 2e emplacement |
| `MAG_Troisieme_Rune` | 120 | MAG minimale pour le 3e emplacement |
| `Afficher_Indication` | true | Bandeau d'information lors d'un changement d'emplacement |
| `Afficher_3_Emplacements_Boutique` | true | Panneau des 3 emplacements dans la boutique de runes |

## Utilisation

- **Boutique de runes** : sur la liste des personnages, ←/→ choisit l'emplacement actif ; « Équiper » et « Retirer » agissent sur cet emplacement.
- **Menu principal › Runes** : ←/→ change l'emplacement actif.
- **Combat › Runes** : la fenêtre affiche les 3 emplacements ; A valide la rune et ouvre ses sorts, B revient aux commandes.

## Changements de la version 0.9.8-r158 (d'après le log du 1er test)

- **4e sort inaccessible** : le sélecteur laissait `magic_kazu` (nombre de sorts) à 3 en ouvrant la liste des sorts ; le curseur revenait en haut après le 3e. Il reçoit maintenant le vrai nombre de sorts.
- **Runes qui se superposent** : la fenêtre Runes reprenait la largeur mémorisée sur un autre personnage (ex. 379 au lieu de 452 pour « Rune de pie-grièche »), et le texte débordait. Seule la hauteur est maintenant modifiée.
- **Passifs** : Double-Beat, Tueur, Contre, Tortue et Brume ne se déclenchaient jamais (aucune ligne `PASSIF` dans le log). La rune secondaire est maintenant présentée à tous ses porteurs pendant l'appel natif, puis la rune active est restaurée. Chaque activation est écrite une fois dans le log (`R108 PASSIF 3-SLOTS`).

## Changements de la version 0.9.7-r157 (correctifs)

**Stockage (runes perdues ou dupliquées, passifs inactifs, boutique)**

Jusqu'ici, l'état des 3 runes était caché en permanence dans le champ natif `monsyo_exp`. Or le moteur lit ce champ : il bloquait « Retirer » en boutique et cassait Pie-grièche. Le mod devait donc le remettre à 0 par moments, et l'état disparaissait alors de la mémoire du jeu.

Désormais :

- l'état est conservé par le mod, et `monsyo_exp` garde sa valeur vanilla pendant le jeu ;
- le format compact n'est écrit que pendant la copie vers la sauvegarde, puis relu au chargement ;
- l'état est restauré même si la sauvegarde échoue ;
- les runes de tous les personnages sont relues au chargement, pas seulement celles du porteur de Soul Eater ;
- un changement d'équipement fait par le jeu (boutique, événement) est toujours enregistré dans l'emplacement actif, avant toute écriture. Avant, ouvrir le menu Runes en combat pouvait réécrire une ancienne rune par-dessus une rune achetée en boutique ;
- les runes présentées temporairement au moteur (sélecteur Pie-grièche, passifs) ne sont jamais prises pour un changement d'équipement.

**Boutique et menu principal**

La détection du contexte testait le combat en premier. Les zones de combat du jeu étant statiques, le mod pouvait se croire en combat pour le reste de la session après le premier combat. La boutique (panneau, ←/→, Retirer) et le menu principal étaient alors ignorés. L'ordre est maintenant : boutique, puis menu Runes, puis combat.

**Passifs**

- L'addon lit l'état directement dans le mod principal. Avant, il le décodait depuis `monsyo_exp`, souvent remis à 0, et les emplacements 2/3 devenaient invisibles.
- Le personnage concerné est choisi d'après le nom des paramètres natifs. Avant, le premier entier venu était pris : un index de monstre pouvait être confondu avec un index de personnage (Contre/Tortue appliqués au mauvais personnage).
- La vraie signature de chaque point natif est écrite une fois dans le log (`R157 PASSIF signature native`).

**Robustesse**

Chaque patch Harmony est appliqué séparément. Un point natif introuvable ne désactive plus tout le mod, et l'erreur est écrite dans le log.

## Limites connues

- Le comportement en jeu n'a pas pu être testé ici. Le code a été recompilé contre des assemblies stub dont les signatures sont identiques à celles des DLL d'origine.
- Si l'on revient à l'écran titre puis qu'on lance une **nouvelle partie** sans charger, les emplacements 2/3 de la partie précédente peuvent rester en mémoire. Ce problème existait déjà. Charger une sauvegarde remet tout à zéro.

## Signaler un problème

Envoyer `BepInEx/LogOutput.log` (lignes `R157`, `R107`, `R108`, `R156`) avec la description de l'action faite juste avant.

## Compiler

```
# sans le jeu (stubs du dossier stubs/) :
dotnet build -c Release

# avec les vraies DLL du jeu :
dotnet build -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Suikoden I and II HD Remaster"
```

Les DLL sont produites dans `build/`. Le dossier `stubs/` ne sert qu'à la compilation : ne **jamais** copier ces DLL dans BepInEx.
