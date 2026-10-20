# Сторонние материалы примера

Лицензия MIT из [LICENSE.md](LICENSE.md) применяется к исходному коду и документации примера, созданным Vadim Burym. Перечисленные ниже материалы и их производные сохраняют права и условия исходных правообладателей.

## Источники

| Пакет | Автор / издатель | Файлы в примере | Условия на странице пакета |
| --- | --- | --- | --- |
| [Mini Legion Footman PBR HP Polyart](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/mini-legion-footman-pbr-hp-polyart-86576) | Dungeon Mason | `Content/Models/Footman/Footman_Polyart.fbx`, включая встроенные анимации | Standard Unity Asset Store EULA |
| [Mini Legion Grunt PBR HP Polyart](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/mini-legion-grunt-pbr-hp-polyart-98187) | Dungeon Mason | `Content/Models/Orc/GruntPolyart.fbx`, включая встроенные анимации; `Content/Models/Orc/PolyartStandard.png` | Standard Unity Asset Store EULA |
| [Gridbox Prototype Materials](https://assetstore.unity.com/packages/2d/textures-materials/gridbox-prototype-materials-129127) | Ciathyza в метаданных импортированного пакета; на текущей странице издатель Surreal Cosmic | `Content/Battlefield/prototype_512x512_blue1.png`, `prototype_512x512_normal.png`; `Content/Materials/Prototype_512x512_Blue1.mat` | Standard Unity Asset Store EULA |

Источники сверены 18 октября 2026 года. В `.meta` сохранены Asset Store product ID: Footman **86576**, Grunt **98187**, Gridbox **129127**. Это позволяет проверить происхождение даже после переименования и переноса файлов.

## VAT и производные данные

`Content/Animation/VAT_Position.exr`, `VAT_Normal.exr`, `VAT_Meta.exr` и данные клипов в `VATAnimationLibrary.asset` получены из моделей и анимаций персонажей. Изменение формата хранения не переводит эти материалы под MIT. Материал `_SimpleVAT.mat` ссылается на стороннюю текстуру и VAT-данные; его наличие также не дает новых прав на них.

## Использование и распространение

Применяется [Unity Asset Store EULA](https://unity.com/legal/as-terms), в частности разделы 2.2.1 и 2.2.1.1. Условия различают использование ассета внутри продукта и распространение самого ассета. На проверенных страницах все три пакета бесплатные, но опубликованы под EULA, а не под MIT или CC0.

У каждого пользователя должны быть необходимые права на используемые пакеты. Получайте их через официальные страницы Asset Store. MIT этого примера и указание авторов не разрешают раздачу FBX, текстур, анимаций и их производных как исходников. Отдельное разрешение авторов на такую поставку в репозитории не приложено.

Unity, пакеты Entities, Burst, Collections, Mathematics, Entities Graphics, URP и Input System распространяются по собственным условиям. Их лицензии доступны в установленных пакетах и на страницах Unity.
