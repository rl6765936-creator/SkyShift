# SkyShift vehicle catalog — image inventory and manifest

Prepared on 2026-10-10 from the folder `Downloads\Cars-with-propellers` and the
frontend folder `Downloads\SkyShift-React-updated (7)\skyshift-react\public\catalog`.
No image was created, edited, copied or deleted.

## Important

- Every image is an **edited picture with added propellers**. No road car in this
  catalog can fly. SkyShift stores **no flight specifications**.
- Every catalog item has **seats**, a **maximum road speed** and short **English and
  Hebrew descriptions**. No value is missing.
- Each of the two specifications has its own accuracy flag:
  - `Verified`: taken from an official manufacturer document (sources below).
  - `Illustrative`: a plausible **SkyShift sample value**. It is **not** a
    manufacturer specification and must be labelled as such in the UI.
- **Identification is separate from specification accuracy.** `Identified` /
  `VisualInspiration` says how sure we are about the manufacturer and model.
  A correctly identified car can still have illustrative figures.
  `VisualInspiration` items only carry illustrative figures.
- Concept cars and camouflaged prototypes have no production specification for
  the pictured car, so their figures are illustrative.

## Inventory summary

- 71 files, all images (56 WebP, 15 JPEG). No subfolders and no non-image files.
- **0 byte-identical duplicates** (SHA-256).
- **Visual duplicates:** the 15 GUID-named `.jpg` files are "take-off" edits of
  photos that also exist as `.webp`. Several vehicles have more than one photo.
  In total the 71 files show **47 distinct vehicles**.
- The frontend's 22 files `car-01.webp` … `car-22.webp` are **background-replaced
  edits** of 22 of those vehicles. They are not byte copies, so they were matched
  visually.
- `248799 - Copy.webp` has no matching `248799.webp` in the folder.

## Seeded catalog (22 items) — original → model → frontend path

All 22 frontend assets already exist. **There are no missing frontend assets for
seeded items.** Totals: seats are Verified for 3 items (6, 19, 21) and Illustrative
for 19; maximum road speed is Verified for 2 items (4, 21) and Illustrative for 20.

| Id | Frontend path | Source original(s) used for the edit | Other photos of the same car | Manufacturer | Model | Identification | Seats (accuracy) | Max road speed km/h (accuracy) |
|---|---|---|---|---|---|---|---|---|
| 1 | /catalog/car-01.webp | 14ab6118-a794-473c-8e0f-66b2d64d5aaf.jpg | 246648.webp | Audi | Q3 (2nd generation) | Identified (plate "Q 3200", body) | 5 (Illustrative) | 210 (Illustrative) |
| 2 | /catalog/car-02.webp | 317f8223-4523-4a60-ab3a-d57abba04b87.jpg | 234901.webp, 424c7325-…jpg, ed5fe993-…jpg | Geely | Geometry E (uncertain) | VisualInspiration | 5 (Illustrative) | 150 (Illustrative) |
| 3 | /catalog/car-03.webp | 3d8c9377-14c5-41ac-8614-1dc58b84ac95.jpg | 63372.webp | Koenigsegg | Regera | Identified | 2 (Illustrative) | 400 (Illustrative) |
| 4 | /catalog/car-04.webp | 3f15c2d3-1fa5-4156-9453-6e6de6b89015.jpg | 263441.webp | Bugatti | Chiron Super Sport | Identified | 2 (Illustrative) | 440 (Verified) |
| 5 | /catalog/car-05.webp | 4632f880-9a96-431e-8ce5-7919f8bb0c3f.jpg | 27765.webp | Tesla | Model X | Identified | 5 (Illustrative) | 250 (Illustrative) |
| 6 | /catalog/car-06.webp | 237981.webp | 280847.webp, 280852.webp | BMW | i Vision Circular (concept) | Identified | 4 (Verified) | 180 (Illustrative) |
| 7 | /catalog/car-07.webp | 248714.webp | — | Mercedes-Benz | CLE Coupé (camouflaged prototype, uncertain) | VisualInspiration | 4 (Illustrative) | 250 (Illustrative) |
| 8 | /catalog/car-08.webp | 279664.webp | — | Mercedes-Benz | Concept CLA Class (concept) | Identified | 5 (Illustrative) | 210 (Illustrative) |
| 9 | /catalog/car-09.webp | 7ed7831e-8fa5-4047-a45c-885766c97017.jpg | 543715.webp | Nissan | N7 | Identified ("N7" badge) | 5 (Illustrative) | 170 (Illustrative) |
| 10 | /catalog/car-10.webp | 283575.webp (closest; the frontend image shows a different angle) | — | BMW | Vision Neue Klasse (concept) | Identified ("Neue Klasse" plate) | 5 (Illustrative) | 200 (Illustrative) |
| 11 | /catalog/car-11.webp | 9305fbcc-c905-4e6b-aa04-cac6637f3935.jpg | 267477.webp | BMW | 5 Series (G30, art livery) | Identified (variant unknown) | 5 (Illustrative) | 250 (Illustrative) |
| 12 | /catalog/car-12.webp | 9c410ab5-0833-4911-9d93-a7e5537bd79c.jpg | 259506.webp | Lancia | Pu+Ra HPE (concept) | Identified | 4 (Illustrative) | 180 (Illustrative) |
| 13 | /catalog/car-13.webp | a0c33ed3-31f3-4001-81f6-1d681ddac2d8.jpg | 251701.webp | Bestune | Xiaoma (uncertain) | VisualInspiration | 4 (Illustrative) | 100 (Illustrative) |
| 14 | /catalog/car-14.webp | a53038e7-5f05-44e3-9e04-0cc69011557c.jpg | 225796.webp | Toyota | Prius Plug-in Hybrid (5th gen.) | Identified ("Prius Plug-in Hybrid" plate) | 5 (Illustrative) | 175 (Illustrative) |
| 15 | /catalog/car-15.webp | 265414.webp (closest; different angle) | — | Škoda | Kodiaq (2nd gen., camouflaged prototype) | Identified (variant unknown) | 5 (Illustrative) | 200 (Illustrative) |
| 16 | /catalog/car-16.webp | 248717.webp | — | Maserati | GranCabrio (camouflaged prototype) | Identified (variant unknown) | 4 (Illustrative) | 300 (Illustrative) |
| 17 | /catalog/car-17.webp | bd2340a7-d1cb-4007-b4de-23e8ebe3f21a.jpg | 260749.webp | Unidentified | Hypercar (unidentified) | VisualInspiration | 2 (Illustrative) | 350 (Illustrative) |
| 18 | /catalog/car-18.webp | 277059.webp | — | Mercedes-Benz | EQB | Identified (plate "EQ 2430E") | 5 (Illustrative) | 160 (Illustrative) |
| 19 | /catalog/car-19.webp | c9393ccd-d2a8-4153-8430-9bff25e25ea4.jpg | 248799 - Copy.webp | MINI | Countryman (3rd gen., camouflaged prototype) | Identified | 5 (Verified) | 210 (Illustrative) |
| 20 | /catalog/car-20.webp | d5103f06-d98c-4808-863f-cd050daeb4f7.jpg | 232053.webp | Chery | QQ Ice Cream | Identified (plate text) | 4 (Illustrative) | 100 (Illustrative) |
| 21 | /catalog/car-21.webp | 278799.webp | — | MINI | Cooper E (3-door) | Identified (reference variant Cooper E; photo could be Cooper SE) | 4 (Verified) | 160 (Verified) |
| 22 | /catalog/car-22.webp | 282480.webp | — | Peugeot | E-3008 | Identified ("NEW E-3008" plate) | 5 (Illustrative) | 170 (Illustrative) |

### Corrections to the earlier draft in `backend-update` (not applied)

That draft's names did not match its own images: car-01 "Audi Q8" → Audi Q3,
car-02 "Geometry C" → Geometry E (uncertain), car-03 "Ferrari F8 Spider" → Koenigsegg Regera,
car-05 "Model Y" → Model X, car-06 "BMW 3 Series" → BMW i Vision Circular,
car-07 "BMW 5 Series" → Mercedes CLE prototype, car-08 "Hyundai Elantra" → Mercedes Concept CLA,
car-09 "BYD Seal" → Nissan N7, car-10 "Škoda Octavia" → BMW Vision Neue Klasse,
car-11 "BMW M5" → BMW 5 Series (G30), car-15 "BMW X7" → Škoda Kodiaq prototype,
car-16 "Porsche 911" → Maserati GranCabrio prototype, car-17 "BMW M4" → unidentified hypercar,
car-18 "VW Tiguan" → Mercedes EQB, car-19 "VW ID. Buzz" → MINI Countryman prototype,
car-20 "Honda e" → Chery QQ Ice Cream. Its speeds were labelled "simulated" and are not used.

## Verified specifications and sources

| Item | Value | Official source |
|---|---|---|
| 4 Bugatti Chiron Super Sport | Road top speed 440 km/h, electronically limited | Bugatti Newsroom, "Bugatti Chiron Super Sport – customers experience the new hyper sports car for the first time": https://newsroom.bugatti.com/press-releases/bugatti-chiron-super-sport-customers-experience-the-new-hyper-sports-car-for-the-first-time (also https://newsroom.bugatti.com/press-releases/the-bugatti-chiron-super-sport-the-quintessence-of-luxury-and-speed) |
| 6 BMW i Vision Circular | 4 seats ("The four-seater is fully electrically powered"), no top speed published | BMW Group PressClub, 6 Sept 2021: https://www.press.bmwgroup.com/usa/article/detail/T0341640EN_US/the-bmw-i-vision-circular |
| 19 MINI Countryman | 5 seats | BMW Group PressClub technical specifications MINI Countryman C, 03/2024: https://www.press.bmwgroup.com/slovenia/article/attachment/T0448255SL/628502 (top speed 212 km/h there; 194 km/h in https://www.press.bmwgroup.com/asia/article/attachment/T0445885EN/623886, so the speed is stored as Illustrative) |
| 21 MINI Cooper E | 4 seats, road top speed 160 km/h | BMW Group PressClub "Technical Specifications. MINI Cooper E.", 11/2023: https://www.press.bmwgroup.com/global/article/attachment/T0448246EN/628481 |

All other figures are **Illustrative SkyShift data**: plausible values for the
vehicle type, chosen without an official source. To turn one into a verified
value later, take seats and road top speed from **one** official variant
document, then add a new migration that updates only that catalog row and sets
its accuracy to `Verified`.

## Optional extra vehicles (not seeded; no frontend asset yet)

These are distinct vehicles in the folder that are not in the catalog. To add one,
copy the source file to the proposed path (converting to WebP if needed), research
its specs, and add a new migration. **All of these frontend assets are missing.**

| Proposed path | Source file(s) | Vehicle (visual identification) |
|---|---|---|
| /catalog/car-23.webp | 218613.webp | BYD Seal |
| /catalog/car-24.webp | 256020.webp | Mercedes-Maybach EQS SUV |
| /catalog/car-25.webp | 257726.webp | McLaren 750S Spider + coupé (two cars in one image) |
| /catalog/car-26.webp | 255166.webp | Aston Martin Vulcan (track-only, not road-legal: no road speed applies) |
| /catalog/car-27.webp | 266561.webp | Peugeot e-208 |
| /catalog/car-28.webp | 268711.webp | BYD Atto 3 |
| /catalog/car-29.webp | 280818.webp | Lotus Emeya |
| /catalog/car-30.webp | 280863/280864/280865/280866.webp | Renault EZ-Ultimo (concept) |
| /catalog/car-31.webp | 280872.webp, 280879.webp | Audi skysphere (concept) |
| /catalog/car-32.webp | 280882.webp, 280883.webp | Jaguar Future-Type (concept) |
| /catalog/car-33.webp | 280884.webp, 280886.webp | Jaguar concept (exact concept uncertain) |
| /catalog/car-34.webp | 280935.webp, 280938.webp | Mercedes-Benz VISION AVTR (concept) |
| /catalog/car-35.webp | 304459.webp | Estate car in art livery (unidentified) |
| /catalog/car-36.webp | 309341.webp | Renault Taliant |
| /catalog/car-37.webp | 316673.webp | Renault 5 E-Tech electric |
| /catalog/car-38.webp | 320551.webp | smart #1 |
| /catalog/car-39.webp | 320842.webp | Peugeot 2008 |
| /catalog/car-40.webp | 324190.webp | Tesla Model Y |
| /catalog/car-41.webp | 327064.webp | Audi R8 |
| /catalog/car-42.webp | 327608.webp | Volkswagen Golf (Mk8, wrapped) |
| /catalog/car-43.webp | 333679.webp | Porsche Macan Electric (two cars) |
| /catalog/car-44.webp | 342697.webp | Alfa Romeo Stelvio |
| /catalog/car-45.webp | 40657.webp | Volkswagen Passat (B1, classic) |
| /catalog/car-46.webp | 78843.webp | BMW i4 |
| /catalog/car-47.webp | 8yJ59sw6PUAUBq4CPu9hqD-970-80.jpg.webp | Concept car (unidentified) |

## Existing fleet vehicles — linking plan (manual, not automatic)

The migration leaves every existing vehicle **unlinked** (`CatalogItemId = NULL`)
and does not change its data. All 22 catalog items stay available until a vehicle
is created from them. To link a legacy vehicle later, do it only after a fresh
backup and a manual review:

```sql
-- 1) Review candidates (read-only).
SELECT v.Id, v.Status, v.ModelName, v.ImageUrl, c.Id AS SuggestedCatalogItemId, c.Manufacturer, c.ModelName AS CatalogModel
FROM Vehicles v
LEFT JOIN VehicleCatalogItems c ON c.ImageUrl = v.ImageUrl
WHERE v.CatalogItemId IS NULL
ORDER BY v.Id;

-- 2) Only for pairs you confirmed. Save the vehicle's current values first
--    (step 1 output). The unique index rejects reusing a catalog item.
BEGIN TRANSACTION;
UPDATE v SET
    v.CatalogItemId = c.Id, v.Manufacturer = c.Manufacturer, v.ModelName = c.ModelName,
    v.MaxSpeedKmh = c.MaxRoadSpeedKmh, v.Seats = c.Seats, v.ImageUrl = c.ImageUrl,
    v.Description = c.Description, v.DescriptionHe = c.DescriptionHe,
    v.CatalogIdentification = c.Identification,
    v.SeatsAccuracy = c.SeatsAccuracy, v.MaxRoadSpeedAccuracy = c.MaxRoadSpeedAccuracy
FROM Vehicles v JOIN VehicleCatalogItems c ON c.Id = @catalogItemId
WHERE v.Id = @vehicleId AND v.CatalogItemId IS NULL;
-- check @@ROWCOUNT = 1, then COMMIT; otherwise ROLLBACK.
COMMIT;
```

Linking copies the catalog's values together with their accuracy flags, because
the API reports `maxRoadSpeedKmh` and the accuracy only for catalog-linked
vehicles. Do not set `CatalogItemId` alone, or an old free-form speed would be
shown as a catalog road speed without the correct Verified/Illustrative label.
IDs, status, creation time, assignments and flights are not touched by this
statement.

A vehicle that was already created from the catalog **before**
`CompleteCatalogSpecifications` keeps its stored values (possibly empty seats or
speed, and no accuracy). The migration deliberately does not change fleet rows.
To refresh such a vehicle, use the same reviewed statement with the extra
condition removed (`AND v.CatalogItemId IS NULL` → `AND v.CatalogItemId = @catalogItemId`),
after a backup.
