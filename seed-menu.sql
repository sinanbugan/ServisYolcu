-- ============================================================
-- Menu & MenuRole Seed Data
-- UserRole: Admin=0, Driver=1, Passenger=2
-- ============================================================

-- Menus temizle (MenuRoles cascade ile silinir)
DELETE FROM "MenuRoles";
DELETE FROM "Menus";

ALTER SEQUENCE "Menus_Id_seq"     RESTART WITH 1;
ALTER SEQUENCE "MenuRoles_Id_seq" RESTART WITH 1;

-- ============================================================
-- ANA MENÜLER
-- ============================================================
INSERT INTO "Menus" ("Key", "Label", "Icon", "Path", "Order", "ParentId", "IsActive", "CreatedAt")
VALUES
  ('dashboard',       'Dashboard',       'dashboard',        '/dashboard',        1,  NULL, TRUE, NOW()),
  ('trips',           'Seferler',        'directions_bus',   '/trips',            2,  NULL, TRUE, NOW()),
  ('reservations',    'Rezervasyonlar',  'event_seat',       '/reservations',     3,  NULL, TRUE, NOW()),
  ('routes',          'Güzergahlar',     'route',            '/routes',           4,  NULL, TRUE, NOW()),
  ('driver-panel',    'Şoför Paneli',    'steering',         '/driver-panel',     5,  NULL, TRUE, NOW()),
  ('users',           'Kullanıcılar',    'group',            '/users',            6,  NULL, TRUE, NOW()),
  ('reports',         'Raporlar',        'assessment',       '/reports',          7,  NULL, TRUE, NOW()),
  ('settings',        'Ayarlar',         'settings',         '/settings',         8,  NULL, TRUE, NOW());

-- ALT MENÜLER (reports altında)
INSERT INTO "Menus" ("Key", "Label", "Icon", "Path", "Order", "ParentId", "IsActive", "CreatedAt")
VALUES
  ('reports-trips',        'Sefer Raporları',        'bar_chart',    '/reports/trips',        1, 7, TRUE, NOW()),
  ('reports-reservations', 'Rezervasyon Raporları',  'pie_chart',    '/reports/reservations', 2, 7, TRUE, NOW()),
  ('reports-revenue',      'Gelir Raporları',        'attach_money', '/reports/revenue',      3, 7, TRUE, NOW());

-- ALT MENÜLER (settings altında)
INSERT INTO "Menus" ("Key", "Label", "Icon", "Path", "Order", "ParentId", "IsActive", "CreatedAt")
VALUES
  ('settings-profile',  'Profil',        'person',     '/settings/profile',  1, 8, TRUE, NOW()),
  ('settings-password', 'Şifre Değiştir','lock',       '/settings/password', 2, 8, TRUE, NOW());

-- ============================================================
-- MenuRole ATAMALAR
-- Admin (0): tüm menüler
-- Driver (1): dashboard, trips, driver-panel, settings + alt menüleri
-- Passenger (2): dashboard, trips, reservations, settings + alt menüleri
-- ============================================================

-- Dashboard → Admin(0), Driver(1), Passenger(2)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (1, 0), (1, 1), (1, 2);

-- Seferler → Admin(0), Driver(1), Passenger(2)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (2, 0), (2, 1), (2, 2);

-- Rezervasyonlar → Admin(0), Passenger(2)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (3, 0), (3, 2);

-- Güzergahlar → Admin(0)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (4, 0);

-- Şoför Paneli → Admin(0), Driver(1)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (5, 0), (5, 1);

-- Kullanıcılar → Admin(0)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (6, 0);

-- Raporlar (ana) → Admin(0)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (7, 0);

-- Ayarlar (ana) → Admin(0), Driver(1), Passenger(2)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (8, 0), (8, 1), (8, 2);

-- Alt: Sefer Raporları → Admin(0)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (9, 0);

-- Alt: Rezervasyon Raporları → Admin(0)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (10, 0);

-- Alt: Gelir Raporları → Admin(0)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (11, 0);

-- Alt: Profil → Admin(0), Driver(1), Passenger(2)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (12, 0), (12, 1), (12, 2);

-- Alt: Şifre Değiştir → Admin(0), Driver(1), Passenger(2)
INSERT INTO "MenuRoles" ("MenuId", "Role") VALUES
  (13, 0), (13, 1), (13, 2);
