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
DELETE FROM "Routes";
DELETE FROM "Users";

-- Sequence'leri sıfırla (PostgreSQL)
ALTER SEQUENCE "Users_Id_seq"        RESTART WITH 1;
ALTER SEQUENCE "Routes_Id_seq"       RESTART WITH 1;
ALTER SEQUENCE "Trips_Id_seq"        RESTART WITH 1;
ALTER SEQUENCE "Reservations_Id_seq" RESTART WITH 1;

-- ============================================================
-- USERS
-- ============================================================
INSERT INTO "Users" ("FirstName", "LastName", "Email", "PasswordHash", "PhoneNumber", "Role", "IsActive", "CreatedAt")
VALUES
  -- Admin
  ('Ahmet',   'Yılmaz',  'ahmet.yilmaz@servisyolcu.com',  '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000001', 0, TRUE, NOW() - INTERVAL '30 days'),
  -- Şoförler
  ('Mehmet',  'Demir',   'mehmet.demir@servisyolcu.com',  '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000002', 1, TRUE, NOW() - INTERVAL '25 days'),
  ('Ali',     'Kaya',    'ali.kaya@servisyolcu.com',      '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000003', 1, TRUE, NOW() - INTERVAL '20 days'),
  -- Yolcular
  ('Ayşe',   'Çelik',   'ayse.celik@example.com',        '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000004', 2, TRUE, NOW() - INTERVAL '15 days'),
  ('Fatma',  'Şahin',   'fatma.sahin@example.com',       '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000005', 2, TRUE, NOW() - INTERVAL '12 days'),
  ('Mustafa','Arslan',  'mustafa.arslan@example.com',     '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000006', 2, TRUE, NOW() - INTERVAL '10 days'),
  ('Zeynep', 'Koç',     'zeynep.koc@example.com',        '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000007', 2, TRUE, NOW() - INTERVAL '8 days'),
  ('Emre',   'Yıldız',  'emre.yildiz@example.com',       '$2a$11$K8X2tEdH3R0y.wQ7fVzVOuDXh3mQ9SbE4RlNiKqPwZ1aXvYjG3C7y', '05321000008', 2, TRUE, NOW() - INTERVAL '5 days');

-- ============================================================
-- ROUTES
-- ============================================================
INSERT INTO "Routes" ("Name", "StartPoint", "EndPoint", "PricePerSeat", "IsActive", "CreatedAt")
VALUES
  ('İstanbul - Ankara',   'İstanbul',  'Ankara',   350.00, TRUE, NOW() - INTERVAL '28 days'),
  ('Ankara - İzmir',      'Ankara',    'İzmir',    280.00, TRUE, NOW() - INTERVAL '28 days'),
  ('İstanbul - Bursa',    'İstanbul',  'Bursa',    120.00, TRUE, NOW() - INTERVAL '25 days'),
  ('Bursa - Antalya',     'Bursa',     'Antalya',  420.00, TRUE, NOW() - INTERVAL '22 days'),
  ('İzmir - Bodrum',      'İzmir',     'Bodrum',   180.00, TRUE, NOW() - INTERVAL '20 days');

-- ============================================================
-- TRIPS  (DriverId: 2=Mehmet, 3=Ali)
-- ============================================================
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
INSERT INTO "Reservations" ("SeatCount", "Status", "TripId", "PassengerId", "CreatedAt")
VALUES
  -- Sefer 1 (geçmiş - tüm koltuklar dolu)
  (2, 1, 1, 4, NOW() - INTERVAL '11 days'),  -- Ayşe, Confirmed
  (3, 1, 1, 5, NOW() - INTERVAL '11 days'),  -- Fatma, Confirmed
  (1, 2, 1, 6, NOW() - INTERVAL '10 days'),  -- Mustafa, Cancelled
  -- Sefer 2 (geçmiş - 3 koltuk kaldı)
  (2, 1, 2, 7, NOW() - INTERVAL '6 days'),   -- Zeynep, Confirmed
  (1, 1, 2, 8, NOW() - INTERVAL '6 days'),   -- Emre, Confirmed
  -- Sefer 3 (bugün - 10 koltuk kaldı)
  (2, 1, 3, 4, NOW() - INTERVAL '1 day'),    -- Ayşe, Confirmed
  (3, 0, 3, 6, NOW() - INTERVAL '1 day'),    -- Mustafa, Pending
  -- Sefer 4 (gelecek - 28 koltuk kaldı)
  (1, 0, 4, 5, NOW()),                       -- Fatma, Pending
  (1, 0, 4, 8, NOW());                       -- Emre, Pending
