-- ============================================================
-- ServisYolcu Seed Data Script
-- UserRole  : Admin=0, Driver=1, Passenger=2
-- ResStatus : Pending=0, Confirmed=1, Cancelled=2
-- PasswordHash: BCrypt("Password123!")
-- ============================================================

-- Mevcut verileri temizle (bağımlılık sırasına göre)
DELETE FROM "Reservations";
DELETE FROM "Trips";
DELETE FROM "RefreshTokens";
DELETE FROM "Stops";
DELETE FROM "Routes";
DELETE FROM "Users";
DELETE FROM "Companies";

-- Sequence'leri sıfırla (PostgreSQL)
ALTER SEQUENCE "Users_Id_seq"        RESTART WITH 1;
ALTER SEQUENCE "Routes_Id_seq"       RESTART WITH 1;
ALTER SEQUENCE "Trips_Id_seq"        RESTART WITH 1;
ALTER SEQUENCE "Reservations_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Stops_Id_seq"        RESTART WITH 1;
ALTER SEQUENCE "Companies_Id_seq"    RESTART WITH 1;

-- ============================================================
-- COMPANIES
-- ============================================================
INSERT INTO "Companies" ("Name", "CompanyCode", "IsActive", "CreatedAt")
VALUES
  ('Servis Yolcu A.Ş.', 'SERVISYOLCU', TRUE, NOW() - INTERVAL '60 days'),
  ('Test Şirketi', 'TEST', TRUE, NOW() - INTERVAL '30 days');

-- ============================================================
-- USERS
-- ============================================================
INSERT INTO "Users" ("FirstName", "LastName", "Email", "PasswordHash", "PhoneNumber", "Role", "RefNumber", "CompanyId", "IsActive", "CreatedAt")
VALUES
  -- Admin (Company 1)
  ('Ahmet',   'Yılmaz',  'ahmet.yilmaz@servisyolcu.com',  '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000001', 0, NULL, 1, TRUE, NOW() - INTERVAL '30 days'),
  -- Şoförler (Company 1)
  ('Mehmet',  'Demir',   'mehmet.demir@servisyolcu.com',  '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000002', 1, 'DRV001', 1, TRUE, NOW() - INTERVAL '25 days'),
  ('Ali',     'Kaya',    'ali.kaya@servisyolcu.com',      '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000003', 1, 'DRV002', 1, TRUE, NOW() - INTERVAL '20 days'),
  -- Yolcular (Company 1)
  ('Ayşe',   'Çelik',   'ayse.celik@example.com',        '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000004', 2, NULL, 1, TRUE, NOW() - INTERVAL '15 days'),
  ('Fatma',  'Şahin',   'fatma.sahin@example.com',       '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000005', 2, NULL, 1, TRUE, NOW() - INTERVAL '12 days'),
  ('Mustafa','Arslan',  'mustafa.arslan@example.com',     '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000006', 2, NULL, 1, TRUE, NOW() - INTERVAL '10 days'),
  ('Zeynep', 'Koç',     'zeynep.koc@example.com',        '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000007', 2, NULL, 1, TRUE, NOW() - INTERVAL '8 days'),
  ('Emre',   'Yıldız',  'emre.yildiz@example.com',       '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000008', 2, NULL, 1, TRUE, NOW() - INTERVAL '5 days');

-- ============================================================
-- ROUTES
-- ============================================================
INSERT INTO "Routes" ("Name", "StartPoint", "EndPoint", "PricePerSeat", "CompanyId", "IsActive", "CreatedAt")
VALUES
  ('İstanbul - Ankara',   'İstanbul',  'Ankara',   350.00, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('Ankara - İzmir',      'Ankara',    'İzmir',    280.00, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('İstanbul - Bursa',    'İstanbul',  'Bursa',    120.00, 1, TRUE, NOW() - INTERVAL '25 days'),
  ('Bursa - Antalya',     'Bursa',     'Antalya',  420.00, 1, TRUE, NOW() - INTERVAL '22 days'),
  ('İzmir - Bodrum',      'İzmir',     'Bodrum',   180.00, 1, TRUE, NOW() - INTERVAL '20 days');

-- ============================================================
-- STOPS
-- ============================================================
INSERT INTO "Stops" ("Name", "Address", "Latitude", "Longitude", "Order", "RouteId", "CompanyId", "IsActive", "CreatedAt")
VALUES
  -- Route 1: İstanbul - Ankara
  ('İstanbul Otogarı', 'İstanbul Avrupa Yakası', 41.0138, 28.9497, 1, 1, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('Sakarya', 'Sakarya İl Merkezi', 40.7569, 30.3781, 2, 1, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('Bolu', 'Bolu İl Merkezi', 40.7358, 31.6061, 3, 1, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('Ankara AŞTİ', 'Ankara AŞTİ', 39.9208, 32.8541, 4, 1, 1, TRUE, NOW() - INTERVAL '28 days'),
  -- Route 2: Ankara - İzmir
  ('Ankara AŞTİ', 'Ankara AŞTİ', 39.9208, 32.8541, 1, 2, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('Konya', 'Konya İl Merkezi', 37.8746, 32.4932, 2, 2, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('Uşak', 'Uşak İl Merkezi', 38.6823, 29.4082, 3, 2, 1, TRUE, NOW() - INTERVAL '28 days'),
  ('İzmir Otogarı', 'İzmir Otogarı', 38.4237, 27.1428, 4, 2, 1, TRUE, NOW() - INTERVAL '28 days'),
  -- Route 3: İstanbul - Bursa
  ('İstanbul Otogarı', 'İstanbul Avrupa Yakası', 41.0138, 28.9497, 1, 3, 1, TRUE, NOW() - INTERVAL '25 days'),
  ('Bursa Otogarı', 'Bursa Otogarı', 40.1826, 29.0669, 2, 3, 1, TRUE, NOW() - INTERVAL '25 days'),
  -- Route 4: Bursa - Antalya
  ('Bursa Otogarı', 'Bursa Otogarı', 40.1826, 29.0669, 1, 4, 1, TRUE, NOW() - INTERVAL '22 days'),
  ('İzmir', 'İzmir İl Merkezi', 38.4237, 27.1428, 2, 4, 1, TRUE, NOW() - INTERVAL '22 days'),
  ('Antalya Otogarı', 'Antalya Otogarı', 36.8969, 30.7133, 3, 4, 1, TRUE, NOW() - INTERVAL '22 days'),
  -- Route 5: İzmir - Bodrum
  ('İzmir Otogarı', 'İzmir Otogarı', 38.4237, 27.1428, 1, 5, 1, TRUE, NOW() - INTERVAL '20 days'),
  ('Muğla', 'Muğla İl Merkezi', 37.2154, 28.3636, 2, 5, 1, TRUE, NOW() - INTERVAL '20 days'),
  ('Bodrum Otogarı', 'Bodrum Otogarı', 37.0344, 27.4305, 3, 5, 1, TRUE, NOW() - INTERVAL '20 days');
INSERT INTO "Trips" ("DepartureTime", "TotalSeats", "AvailableSeats", "VehiclePlate", "IsActive", "RouteId", "DriverId", "CreatedAt")
VALUES
  -- Geçmiş seferler
  (NOW() - INTERVAL '10 days' + TIME '08:00', 30, 0,  '34 ABC 001', TRUE, 1, 2, NOW() - INTERVAL '12 days'),
  (NOW() - INTERVAL '5 days'  + TIME '09:30', 20, 3,  '06 DEF 002', TRUE, 2, 3, NOW() - INTERVAL '7 days'),
  -- Bugün
  (NOW()                      + TIME '14:00', 25, 10, '34 GHI 003', TRUE, 3, 2, NOW() - INTERVAL '2 days'),
  -- Gelecek seferler
  (NOW() + INTERVAL '3 days'  + TIME '07:00', 30, 28, '07 JKL 004', TRUE, 4, 3, NOW() - INTERVAL '1 day'),
  (NOW() + INTERVAL '7 days'  + TIME '10:00', 20, 20, '35 MNO 005', TRUE, 5, 2, NOW()),
  (NOW() + INTERVAL '14 days' + TIME '08:30', 30, 30, '34 PQR 006', TRUE, 1, 3, NOW());

-- ============================================================
-- RESERVATIONS  (PassengerId: 4=Ayşe, 5=Fatma, 6=Mustafa, 7=Zeynep, 8=Emre)
-- ============================================================
INSERT INTO "Reservations" ("SeatCount", "Status", "TripId", "PassengerId", "BoardingStopId", "CreatedAt")
VALUES
  -- Sefer 1 (geçmiş - tüm koltuklar dolu)
  (2, 1, 1, 4, 1, NOW() - INTERVAL '11 days'),  -- Ayşe, Confirmed, İstanbul
  (3, 1, 1, 5, 2, NOW() - INTERVAL '11 days'),  -- Fatma, Confirmed, Sakarya
  (1, 2, 1, 6, 3, NOW() - INTERVAL '10 days'),  -- Mustafa, Cancelled, Bolu
  -- Sefer 2 (geçmiş - 3 koltuk kaldı)
  (2, 1, 2, 7, 5, NOW() - INTERVAL '6 days'),   -- Zeynep, Confirmed, Ankara
  (1, 1, 2, 8, 6, NOW() - INTERVAL '6 days'),   -- Emre, Confirmed, Konya
  -- Sefer 3 (bugün - 10 koltuk kaldı)
  (2, 1, 3, 4, 9, NOW() - INTERVAL '1 day'),    -- Ayşe, Confirmed, İstanbul
  (3, 0, 3, 6, 10, NOW() - INTERVAL '1 day'),   -- Mustafa, Pending, Bursa
  -- Sefer 4 (gelecek - 28 koltuk kaldı)
  (1, 0, 4, 5, 11, NOW()),                      -- Fatma, Pending, Bursa
  (1, 0, 4, 8, 12, NOW());                      -- Emre, Pending, İzmir
