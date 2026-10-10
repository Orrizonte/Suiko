# Suikoden I HD – Trois runes selon la MAG

Mod BepInEx (IL2CPP) pour **Suikoden I & II HD Remaster** (Steam) : jusqu'à 3 emplacements de rune par personnage dans Suikoden I, débloqués selon la MAG, comme dans Suikoden II.

Un seul fichier, `SuikodenThreeRunes.dll`, gère :
- les 3 emplacements, le menu Runes en combat, la boutique de runes, le menu principal et la sauvegarde ;
- les runes passives (Fortune, Double-Beat, Tueur, Rafale…) équipées dans les emplacements 2/3. C'était l'ancien addon `SuikodenThreeRunesPassives`, maintenant intégré.

## Installation

1. BepInEx 6 (IL2CPP) doit déjà être installé et le jeu lancé une fois (dossier `BepInEx/interop` généré).
2. Copier le contenu de `release/` dans le dossier du jeu : `BepInEx/plugins/SuikodenThreeRunes/SuikodenThreeRunes.dll`.
3. **Supprimer l'ancien dossier `BepInEx/plugins/SuikodenThreeRunesPassives`.** S'il est encore présent, les passifs intégrés se désactivent pour ne pas doubler les effets, et un avertissement apparaît dans le log.
4. Facultatif : supprimer `BepInEx/config/fr.suikodenhd.threerunes.passives.cfg`. Les réglages des passifs sont maintenant dans le fichier de config du mod.

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
| `[Passifs] Activer_passifs_3_slots` | true | Les runes passives des emplacements 2/3 agissent |
| `[Passifs] Journaliser_premiere_activation` | true | Ligne `R108 PASSIF 3-SLOTS` dans le log à la 1re activation |
| `[Passifs_IDs] Rune_…` | — | Identifiants natifs des runes passives (ne pas modifier sauf besoin) |

## Utilisation

- **Boutique de runes** : sur la liste des personnages, ←/→ choisit l'emplacement actif ; « Équiper » et « Retirer » agissent sur cet emplacement.
- **Menu principal › Runes** : ←/→ change l'emplacement actif.
- **Combat › Runes** : la fenêtre affiche les 3 emplacements ; A valide la rune et ouvre ses sorts, B revient aux commandes.

## Mod Compétences (SuikodenSkills) – correctifs binaires

Ce mod n'est livré qu'en DLL. Les correctifs sont appliqués directement à la DLL 0.2.52 par `tools/SkillsAlliedMagicPatch` ; la DLL corrigée est dans `release/BepInEx/plugins/SuikodenSkills/`.

- **0.2.53** : Magie et Résistance magique ne s'appliquent plus aux sorts entre alliés (soins).
- **0.2.54** : vérification visible de la Résistance magique. Quand un sort ennemi touche un personnage, le mod Compétences affiche (notification et log) : « Verification Resistance magique : Viktor recoit 120 -> 102 degats (Resistance magique 15%) ». Aucun message lors d'un sort ennemi signifie que le mod ne voit pas ce sort.

## Changements de la version 0.9.24-r173

- **B depuis la liste des sorts ramène au menu de commandes**, par l'annulation native, comme B dans la fenêtre Runes. Le A automatique de la 0.9.23 est retiré. En rouvrant « Runes », le jeu affiche le sélecteur par son propre chemin, identique à la première fois. Ligne de log `R173`.

## Changements de la version 0.9.23-r172

- **Retour par B identique à la première ouverture** : le mod ne reconstruit plus le sélecteur à la main après un B depuis la liste des sorts. Il passe par le chemin natif :
  1. annulation vers le menu de commandes, comme un B dans le sélecteur ;
  2. un seul A automatique sur « Runes ».

  Le sélecteur s'ouvre donc exactement comme la première fois, avec le même curseur. Lignes de log `R172`.
- Retrait des retouches visuelles R163 et R166 à R171 (défilement, barre, alignement du curseur, verrouillage de position), devenues inutiles.

## Changements de la version 0.9.22-r171

- **Régression de la 0.9.21 après B** : en perdant le focus (passage aux sorts), la liste calcule sa position mémorisée `m_unFocusedCellY` à partir de la ligne sélectionnée : -21,75 pour la ligne 1, -75,27 pour la ligne 2 dans le log. Depuis que la ligne 2 est vraiment sélectionnée (barre R170), le retour par B repositionnait la liste avec un décalage d'une ligne. La valeur d'origine est maintenant verrouillée pendant le sélecteur et avant chaque réouverture (ligne de log `R171`).
- La barre de sélection est bien présente dans les cellules Runes (log R170 : `True`).

## Changements de la version 0.9.21-r170

- **Barre de sélection (4e essai, ciblé)** : le diagnostic R169 montre que chaque cellule de liste porte sa propre barre (`m_cursor : UISelectCursor`), activée par `SetSelect(bool)`, et que la liste offre `SetSelectOneCell(int)`. Le mod les appelle maintenant directement sur la fenêtre Runes, puis retire la barre (`ClearSelect`) en quittant le sélecteur. Ligne de log `R170`.

## Changements de la version 0.9.20-r169

- **Barre de sélection (3e essai)** : `IsChoosable` est calculé par le jeu et ne peut pas être modifié. La barre est un objet du gestionnaire de combat, positionné par type de fenêtre : le curseur est maintenant aussi envoyé à la fenêtre de type Emblem (5), et plus seulement Magic (8).
- Diagnostic `R169 membres curseur` : membres liés au curseur, à la sélection et au focus dans le gestionnaire de combat, la fenêtre de combat, la liste et ses cellules.

## Changements de la version 0.9.19-r168

- **Barre de sélection (2e essai)** : désactiver `m_isCursorOFFInFocus` ne suffisait pas. La liste Runes native est aussi marquée non sélectionnable (`IsChoosable=False`) : elle est rendue sélectionnable pendant le sélecteur, puis rétablie à la sortie. Le curseur interne `m_selectedIndex` est maintenant écrit directement, car `SetSelectCell` ne le mettait pas à jour.
- Diagnostic : comparaison avec la liste des Sorts (qui a une barre) au premier affichage, lignes `R168`.

## Changements de la version 0.9.18-r167

- **Barre de sélection visible dans la fenêtre Runes en combat** : la fenêtre native est une fenêtre d'affichage dont le drapeau `m_isCursorOFFInFocus=True` masque la barre de sélection. Il est désactivé tant que le sélecteur 3 runes est ouvert, puis rétabli (ligne de log `R167`).

## Changements de la version 0.9.17-r166

- **Curseur des runes figé après B** : le log montre qu'après un retour par B, la liste gardait parfois son curseur visuel sur la ligne 2 (`m_selectedIndex=1`) alors que le curseur réel était sur la ligne 1 (`wi=0`). Le curseur affiché ne suivait plus ↑/↓. Une fois la fenêtre ouverte, il est maintenant aligné à chaque image sur l'emplacement réel (ligne de log `R166 CURSEUR Runes aligne`).

## Changements de la version 0.9.16-r165

- **Sorts non sélectionnables après un B** : après un retour Sorts → Runes, valider une rune laissait le jeu au niveau « Runes » (`etat=1/2` dans le log au lieu de `2/0`). La liste de sorts s'affichait mais restait bloquée jusqu'à l'annulation complète. Le passage au niveau « Sorts » est maintenant forcé après un retour par B.
- Au retour par B, le curseur visuel de la liste est aligné sur l'emplacement réel avant la réouverture.
- Tir : l'association dans l'emplacement libre fonctionne (log : « personnage 8 a recu Rune de l'air »).

## Changements de la version 0.9.15-r164

- **Lignes fusionnées après B** : le log montre que le bug apparaît quand la fenêtre Runes s'ouvre avec le curseur ailleurs que sur la ligne 1. L'animation d'ouverture du jeu affiche alors la liste à partir de la ligne sélectionnée. Le sélecteur s'ouvre maintenant toujours sur le premier emplacement utilisable, y compris au retour par B.
- **Tir (expérimental)** : avec son emplacement libre actif (`monsyo_id=0`, `exp=0`), le jeu refusait encore. La règle dépend donc du personnage (héros). Pendant les fonctions natives d'association, et seulement si le curseur est sur un porteur de rune fixe dont l'emplacement libre est actif, son numéro de personnage est remplacé le temps de l'appel par celui d'un autre membre de l'équipe, puis rétabli. Option `[Boutique] Associer_porteur_rune_fixe` (activée par défaut) ; ligne de log `R164 association`.

## Changements de la version 0.9.14-r163

- **Tir (ou tout porteur d'une rune fixe) grisé dans « Sur qui voulez-vous équiper ceci ? »** : le log montre que le jeu refuse un personnage dont `monsyo_exp` est non nul ou dont la rune active est fixe. Pendant la boutique, `monsyo_exp` est remis à 0 pour toute l'équipe, et les porteurs d'une rune fixe active (Mangeuse d'âmes, Pie-grièche…) passent sur leur premier emplacement libre. La rune fixe reste en emplacement 1 et l'emplacement actif d'origine est rétabli à la fermeture de la boutique.
- **Le choix hors combat n'influence plus le combat** : au premier ordre de chaque personnage dans un combat, l'emplacement actif repart sur l'emplacement 1 (ou le premier non vide).
- **Lignes fusionnées après B** : la liste native décalait ses données d'une ligne (« 2. » à la place de « 1. »). Ses index de défilement internes sont remis à 0 tant que le sélecteur est ouvert, puis les 3 lignes sont réécrites. Lignes de log `R163 liste Runes` / `R163 DEFILEMENT`.

## Changements de la version 0.9.13-r162

- **Lignes fusionnées après B** : au retour par B avec le curseur sur l'emplacement 2 ou 3, la liste native défilait d'une ligne et la ligne 1 passait sous le titre. Le défilement est maintenant annulé tant que le sélecteur est ouvert ; le log affiche `R162 DEFILEMENT Runes annule` quand c'est le cas.
- **Boutique, « Sur qui voulez-vous équiper ceci ? »** : le panneau affichait les emplacements du propriétaire de l'orbe au lieu du personnage sous le curseur. Il suit maintenant le curseur pendant le choix de la cible, et ←/→ choisit l'emplacement de cette cible.
- **Restauration** : la valeur préremplie de la 0.9.12 est retirée automatiquement, les runes ayant été retrouvées dans la sauvegarde.
- **Diagnostics** pour Tir (« Ça ne peut pas être associé ») : liste des fonctions de la boutique, et champs natifs de la cible sous le curseur.

## Restaurer des runes perdues (0.9.12-r161)

Le fichier `BepInEx/config/fr.suikodenhd.threerunes.cfg` contient une section `[Restauration]` :

```
Runes_a_restaurer = 1:2/4/0; 17:5/18/0; 23:9/28/0
```

Chaque entrée s'écrit `personnage:rune1/rune2/rune3`. Une rune s'écrit par son numéro ou par son nom (ex. `air`, `brume`) ; `-` ou `0` = vide. Au prochain chargement d'une partie, chaque entrée est appliquée une seule fois, puis retirée de la config. Par sécurité, elle n'est appliquée que si :
- la rune actuellement équipée du personnage est celle de l'emplacement 1 ;
- ses emplacements 2 et 3 sont vides.

La valeur par défaut est vide (depuis 0.9.13). Après chaque chargement, le log liste tous les personnages qui portent une rune (`R161 personnages avec runes`), pour retrouver les numéros.

## Changements de la version 0.9.11-r160

- **Lignes de runes qui fusionnent après B** (retour des sorts vers les runes) : la hauteur native de la fenêtre Runes n'est mémorisée qu'une fois, et non plus quand la fenêtre est déjà agrandie. La hauteur 3 lignes est ré-imposée à chaque image tant que le sélecteur est ouvert, car l'animation de reprise du focus la réduisait à une seule ligne.
- Diagnostic boutique : le log indique si la surimpression (OnGUI) est active et quand le panneau des 3 emplacements est dessiné (lignes `R160 diagnostic`).

## Changements de la version 0.9.10-r159

- **Runes du 2e emplacement perdues après sauvegarde** : depuis 0.9.7, l'état des 3 emplacements n'était écrit dans `monsyo_exp` que pendant `CopyGameWorkToSaveData`. Le log montre qu'une sauvegarde a été faite sans passer par ce chemin. L'état est de nouveau gardé **en permanence** en mémoire de jeu, comme dans la version d'origine : toute sauvegarde le contient. Il est remis à 0 uniquement quand le moteur l'exige (boutique ouverte, Pie-grièche pendant le ciblage/l'exécution), puis réécrit automatiquement. Les runes déjà perdues dans une sauvegarde ne peuvent pas être récupérées.
- Chaque sauvegarde écrit une ligne `R159 sauvegarde : …` dans le log, et la liste des fonctions de sauvegarde du jeu est écrite au démarrage (diagnostic).
- L'avertissement « DIRECT-CARRIER : aucune rune magique secondaire » (Pie-grièche seule) n'est plus répété à chaque image.

## Changements de la version 0.9.9-r159

- Les deux mods sont fusionnés en un seul : `SuikodenThreeRunes.dll`.

## Changements de la version 0.9.8-r158 (d'après le log du 1er test)

- **4e sort inaccessible** : le sélecteur laissait `magic_kazu` (nombre de sorts) à 3 en ouvrant la liste des sorts ; le curseur revenait en haut après le 3e. Il reçoit maintenant le vrai nombre de sorts.
- **Runes qui se superposent** : la fenêtre Runes reprenait la largeur mémorisée sur un autre personnage (ex. 379 au lieu de 452 pour « Rune de pie-grièche »), et le texte débordait. Seule la hauteur est maintenant modifiée.
- **Passifs** : Double-Beat, Tueur, Contre, Tortue et Brume ne se déclenchaient jamais (aucune ligne `PASSIF` dans le log). La rune secondaire est maintenant présentée à tous ses porteurs pendant l'appel natif, puis la rune active est restaurée. Chaque activation est écrite une fois dans le log (`R108 PASSIF 3-SLOTS`).

## Changements de la version 0.9.7-r157 (correctifs)

**Stockage (runes perdues ou dupliquées, passifs inactifs, boutique)**

*(Remplacé en 0.9.10 : voir ci-dessus.)* Jusqu'ici, l'état des 3 runes était caché en permanence dans le champ natif `monsyo_exp`. Or le moteur lit ce champ : il bloquait « Retirer » en boutique et cassait Pie-grièche. Le mod devait donc le remettre à 0 par moments, et l'état disparaissait alors de la mémoire du jeu.

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

La DLL est produite dans `build/`. Le dossier `stubs/` ne sert qu'à la compilation : ne **jamais** copier ces DLL dans BepInEx.
