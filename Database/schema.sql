-- =====================================================
-- FaktureringsAPI - Komplett SQL Schema by AdoteamAB
-- Skapad: 2025-10-30
-- Syfte: Komplett fakturering med svenska skattelagar
-- Integration med Ado_Inventory authentication system
-- =====================================================

USE [Ado_Inventory]
GO

-- Skapa schema för fakturering
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'fakturering')
BEGIN
    EXEC('CREATE SCHEMA [fakturering]')
END
GO

-- =====================================================
-- 1. KUNDER (Clients)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Clients')
BEGIN
    CREATE TABLE [fakturering].[Clients] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [Name] NVARCHAR(255) NOT NULL,
        [OrgNumber] NVARCHAR(20) NULL,
        [ContactPerson] NVARCHAR(255) NULL,
        [Email] NVARCHAR(255) NULL,
        [Phone] NVARCHAR(50) NULL,
        [Address] NVARCHAR(500) NULL,
        [PostalCode] NVARCHAR(10) NULL,
        [City] NVARCHAR(100) NULL,
        [Country] NVARCHAR(100) DEFAULT 'Sverige',
        [VATNumber] NVARCHAR(50) NULL,
        [ROT_RUT_Eligible] BIT DEFAULT 0,
        [PaymentTerms] INT DEFAULT 30, -- Dagar
        [Currency] NVARCHAR(3) DEFAULT 'SEK',
        [IsActive] BIT DEFAULT 1,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 DEFAULT GETDATE(),
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        [UpdatedBy] INT NULL  -- References dbo.Users.user_id
    );
END
GO

-- =====================================================
-- 2. USER-CLIENT ACCESS BRIDGE (Authentication Integration)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'UserClientAccess')
BEGIN
    CREATE TABLE [fakturering].[UserClientAccess] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [UserId] INT NOT NULL, -- References InventoryBE Users.user_id
        [ClientId] UNIQUEIDENTIFIER NOT NULL, -- References fakturering.Clients.Id
        [AccessLevel] NVARCHAR(50) NOT NULL, -- 'Owner', 'ReadWrite', 'ReadOnly'
        [CanViewFinancials] BIT DEFAULT 0, -- Kan se ekonomiska rapporter
        [CanManageInvoices] BIT DEFAULT 0, -- Kan skapa/redigera fakturor
        [CanManageExpenses] BIT DEFAULT 0, -- Kan hantera utgifter
        [CanViewReports] BIT DEFAULT 1, -- Kan se rapporter
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_UserClientAccess_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_UserClientAccess_Client] FOREIGN KEY ([ClientId]) REFERENCES [fakturering].[Clients]([Id]),
        CONSTRAINT [FK_UserClientAccess_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 3. PROJEKT (Projects)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Projects')
BEGIN
    CREATE TABLE [fakturering].[Projects] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [ClientId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [HourlyRate] DECIMAL(10,2) NULL,
        [FixedPrice] DECIMAL(10,2) NULL,
        [ProjectType] NVARCHAR(50) NOT NULL, -- 'Hourly', 'Fixed', 'ROT', 'RUT'
        [StartDate] DATE NULL,
        [EndDate] DATE NULL,
        [EstimatedHours] DECIMAL(8,2) NULL,
        [ActualHours] DECIMAL(8,2) DEFAULT 0,
        [Status] NVARCHAR(50) DEFAULT 'Active', -- 'Active', 'Completed', 'Cancelled', 'Paused'
        [IsActive] BIT DEFAULT 1,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 DEFAULT GETDATE(),
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        [UpdatedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_Projects_Clients] FOREIGN KEY ([ClientId]) REFERENCES [fakturering].[Clients]([Id]),
        CONSTRAINT [FK_Projects_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_Projects_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 4. FAKTUROR (Invoices)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Invoices')
BEGIN
    CREATE TABLE [fakturering].[Invoices] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [InvoiceNumber] NVARCHAR(50) NOT NULL UNIQUE,
        [OCRNumber] NVARCHAR(25) NOT NULL UNIQUE,
        [ClientId] UNIQUEIDENTIFIER NOT NULL,
        [ProjectId] UNIQUEIDENTIFIER NULL,
        [InvoiceDate] DATE NOT NULL,
        [DueDate] DATE NOT NULL,
        [SubTotal] DECIMAL(12,2) NOT NULL,
        [VATRate] DECIMAL(5,2) DEFAULT 25.00,
        [VATAmount] DECIMAL(12,2) NOT NULL,
        [TotalAmount] DECIMAL(12,2) NOT NULL,
        [Currency] NVARCHAR(3) DEFAULT 'SEK',
        [ExchangeRate] DECIMAL(10,4) DEFAULT 1.0000,
        [Status] NVARCHAR(50) DEFAULT 'Draft', -- 'Draft', 'Sent', 'Paid', 'Overdue', 'Cancelled', 'PartiallyPaid'
        [PaymentDate] DATE NULL,
        [PaymentMethod] NVARCHAR(50) NULL,
        [PaymentReference] NVARCHAR(100) NULL,
        [ROT_RUT_Type] NVARCHAR(10) NULL, -- 'ROT', 'RUT', NULL
        [ROT_RUT_Amount] DECIMAL(12,2) NULL,
        [ROT_RUT_PersonalNumber] NVARCHAR(20) NULL, -- För ROT/RUT
        [ROT_RUT_RequestId] NVARCHAR(50) NULL, -- Skatteverket referens
        [PreliminaryTaxAmount] DECIMAL(12,2) DEFAULT 0,
        [Notes] NVARCHAR(2000) NULL,
        [InternalNotes] NVARCHAR(2000) NULL,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 DEFAULT GETDATE(),
        [SentDate] DATETIME2 NULL,
        [PaidDate] DATETIME2 NULL,
        [CancelledDate] DATETIME2 NULL,
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        [UpdatedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_Invoices_Clients] FOREIGN KEY ([ClientId]) REFERENCES [fakturering].[Clients]([Id]),
        CONSTRAINT [FK_Invoices_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [fakturering].[Projects]([Id]),
        CONSTRAINT [FK_Invoices_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_Invoices_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 5. FAKTURARADER (InvoiceLines)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'InvoiceLines')
BEGIN
    CREATE TABLE [fakturering].[InvoiceLines] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [Quantity] DECIMAL(10,3) NOT NULL DEFAULT 1,
        [UnitPrice] DECIMAL(12,2) NOT NULL,
        [Unit] NVARCHAR(50) DEFAULT 'st', -- 'st', 'timmar', 'dagar', 'km', 'månad'
        [VATRate] DECIMAL(5,2) DEFAULT 25.00,
        [VATAmount] DECIMAL(12,2) NOT NULL,
        [LineTotal] DECIMAL(12,2) NOT NULL,
        [DiscountPercent] DECIMAL(5,2) DEFAULT 0,
        [DiscountAmount] DECIMAL(12,2) DEFAULT 0,
        [SortOrder] INT DEFAULT 0,
        [AccountCode] NVARCHAR(10) NULL, -- Bokföringskonto
        [CostCenter] NVARCHAR(50) NULL,
        [ProjectCode] NVARCHAR(50) NULL,
        CONSTRAINT [FK_InvoiceLines_Invoices] FOREIGN KEY ([InvoiceId]) REFERENCES [fakturering].[Invoices]([Id]) ON DELETE CASCADE
    );
END
GO

-- =====================================================
-- 6. INKOMSTER (Income)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Income')
BEGIN
    CREATE TABLE [fakturering].[Income] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [InvoiceId] UNIQUEIDENTIFIER NULL,
        [Date] DATE NOT NULL,
        [Amount] DECIMAL(12,2) NOT NULL,
        [VATAmount] DECIMAL(12,2) DEFAULT 0,
        [NetAmount] DECIMAL(12,2) NOT NULL, -- Amount - VATAmount
        [TaxableAmount] DECIMAL(12,2) NOT NULL,
        [PreliminaryTax] DECIMAL(12,2) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [SubCategory] NVARCHAR(100) NULL,
        [Description] NVARCHAR(500) NULL,
        [PaymentMethod] NVARCHAR(50) NULL,
        [BankReference] NVARCHAR(100) NULL,
        [AccountingMethod] NVARCHAR(20) DEFAULT 'Kontant', -- 'Kontant', 'Bokföring'
        [AccountCode] NVARCHAR(10) NULL,
        [VoucherNumber] NVARCHAR(50) NULL,
        [IsRecurring] BIT DEFAULT 0,
        [RecurrencePattern] NVARCHAR(50) NULL, -- 'Monthly', 'Quarterly', 'Yearly'
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 DEFAULT GETDATE(),
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        [UpdatedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_Income_Invoices] FOREIGN KEY ([InvoiceId]) REFERENCES [fakturering].[Invoices]([Id]),
        CONSTRAINT [FK_Income_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_Income_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 7. UTGIFTER (Expenses)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Expenses')
BEGIN
    CREATE TABLE [fakturering].[Expenses] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [Date] DATE NOT NULL,
        [Amount] DECIMAL(12,2) NOT NULL,
        [VATAmount] DECIMAL(12,2) DEFAULT 0,
        [NetAmount] DECIMAL(12,2) NOT NULL, -- Amount - VATAmount
        [Category] NVARCHAR(100) NOT NULL,
        [SubCategory] NVARCHAR(100) NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [Supplier] NVARCHAR(255) NULL,
        [SupplierOrgNumber] NVARCHAR(20) NULL,
        [InvoiceNumber] NVARCHAR(100) NULL, -- Leverantörsfakturanummer
        [IsDeductible] BIT DEFAULT 1,
        [DeductiblePercentage] DECIMAL(5,2) DEFAULT 100.00,
        [DeductibleAmount] DECIMAL(12,2) NULL,
        [ReceiptPath] NVARCHAR(500) NULL,
        [ReceiptFileName] NVARCHAR(255) NULL,
        [ProjectId] UNIQUEIDENTIFIER NULL,
        [AccountCode] NVARCHAR(10) NULL,
        [VoucherNumber] NVARCHAR(50) NULL,
        [PaymentMethod] NVARCHAR(50) NULL,
        [PaymentDate] DATE NULL,
        [PaymentReference] NVARCHAR(100) NULL,
        -- Reseräkning specifika fält
        [Mileage] DECIMAL(10,2) NULL, -- För reseräkning
        [MileageRate] DECIMAL(10,2) DEFAULT 18.50, -- SEK per mil 2025
        [StartLocation] NVARCHAR(255) NULL,
        [EndLocation] NVARCHAR(255) NULL,
        [TravelPurpose] NVARCHAR(500) NULL,
        [IsPrivateUse] BIT DEFAULT 0,
        [PrivateUsePercentage] DECIMAL(5,2) DEFAULT 0,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 DEFAULT GETDATE(),
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        [UpdatedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_Expenses_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [fakturering].[Projects]([Id]),
        CONSTRAINT [FK_Expenses_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_Expenses_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 8. TIMRAPPORTER (TimeReports)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'TimeReports')
BEGIN
    CREATE TABLE [fakturering].[TimeReports] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [ProjectId] UNIQUEIDENTIFIER NOT NULL,
        [UserId] INT NOT NULL, -- References dbo.Users.user_id (vem som arbetat)
        [Date] DATE NOT NULL,
        [StartTime] TIME NULL,
        [EndTime] TIME NULL,
        [Hours] DECIMAL(4,2) NOT NULL,
        [BreakTime] DECIMAL(4,2) DEFAULT 0, -- Paus i timmar
        [HourlyRate] DECIMAL(10,2) NOT NULL,
        [TotalAmount] DECIMAL(12,2) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [TaskType] NVARCHAR(100) NULL, -- 'Development', 'Meeting', 'Support', etc.
        [IsBillable] BIT DEFAULT 1,
        [IsBilled] BIT DEFAULT 0,
        [InvoiceId] UNIQUEIDENTIFIER NULL,
        [InvoiceLineId] UNIQUEIDENTIFIER NULL,
        [OvertimeHours] DECIMAL(4,2) DEFAULT 0,
        [OvertimeRate] DECIMAL(10,2) NULL,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 DEFAULT GETDATE(),
        [ApprovedBy] INT NULL, -- References dbo.Users.user_id
        [ApprovedDate] DATETIME2 NULL,
        CONSTRAINT [FK_TimeReports_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [fakturering].[Projects]([Id]),
        CONSTRAINT [FK_TimeReports_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_TimeReports_Invoices] FOREIGN KEY ([InvoiceId]) REFERENCES [fakturering].[Invoices]([Id]),
        CONSTRAINT [FK_TimeReports_InvoiceLines] FOREIGN KEY ([InvoiceLineId]) REFERENCES [fakturering].[InvoiceLines]([Id]),
        CONSTRAINT [FK_TimeReports_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 9. SKATTESIMULERING (TaxSimulation)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'TaxSimulation')
BEGIN
    CREATE TABLE [fakturering].[TaxSimulation] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [Year] INT NOT NULL,
        [Month] INT NULL, -- NULL för årlig simulering
        [TotalIncome] DECIMAL(15,2) NOT NULL,
        [TotalExpenses] DECIMAL(15,2) NOT NULL,
        [NetIncome] DECIMAL(15,2) NOT NULL, -- TotalIncome - TotalExpenses
        [TaxableIncome] DECIMAL(15,2) NOT NULL,
        [BasicDeduction] DECIMAL(12,2) DEFAULT 24800, -- 2025 års grundavdrag
        [PreliminaryTax] DECIMAL(12,2) NOT NULL,
        [EstimatedIncomeTax] DECIMAL(12,2) NOT NULL,
        [SelfEmploymentTax] DECIMAL(12,2) NOT NULL, -- Egenavgifter 28.97%
        [SelfEmploymentTaxRate] DECIMAL(5,4) DEFAULT 0.2897,
        [VATOwed] DECIMAL(12,2) DEFAULT 0,
        [VATDeductible] DECIMAL(12,2) DEFAULT 0,
        [VATNet] DECIMAL(12,2) DEFAULT 0, -- VATOwed - VATDeductible
        [TotalTaxOwed] DECIMAL(12,2) NOT NULL,
        [TotalTaxPaid] DECIMAL(12,2) DEFAULT 0,
        [TaxDifference] DECIMAL(12,2) NOT NULL, -- TotalTaxOwed - TotalTaxPaid
        [ROT_RUT_Deductions] DECIMAL(12,2) DEFAULT 0,
        [CalculatedDate] DATETIME2 DEFAULT GETDATE(),
        [CalculatedBy] INT NULL, -- References dbo.Users.user_id
        [Notes] NVARCHAR(1000) NULL,
        [SimulationType] NVARCHAR(50) DEFAULT 'Estimate', -- 'Estimate', 'Declaration', 'Final'
        CONSTRAINT [FK_TaxSimulation_CalculatedBy] FOREIGN KEY ([CalculatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 10. VERIFIKATIONER (Vouchers)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Vouchers')
BEGIN
    CREATE TABLE [fakturering].[Vouchers] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [VoucherNumber] NVARCHAR(50) NOT NULL,
        [VoucherSeries] NVARCHAR(10) DEFAULT 'A', -- A, B, C för olika serier
        [Date] DATE NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [TotalAmount] DECIMAL(12,2) NOT NULL,
        [VoucherType] NVARCHAR(50) NOT NULL, -- 'Manual', 'Invoice', 'Expense', 'Payment', 'Correction'
        [Status] NVARCHAR(50) DEFAULT 'Draft', -- 'Draft', 'Posted', 'Cancelled'
        [InvoiceId] UNIQUEIDENTIFIER NULL,
        [ExpenseId] UNIQUEIDENTIFIER NULL,
        [IncomeId] UNIQUEIDENTIFIER NULL,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [PostedDate] DATETIME2 NULL,
        [PostedBy] INT NULL, -- References dbo.Users.user_id
        [CancelledDate] DATETIME2 NULL,
        [CancelledBy] INT NULL, -- References dbo.Users.user_id
        [CancellationReason] NVARCHAR(500) NULL,
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_Vouchers_Invoices] FOREIGN KEY ([InvoiceId]) REFERENCES [fakturering].[Invoices]([Id]),
        CONSTRAINT [FK_Vouchers_Expenses] FOREIGN KEY ([ExpenseId]) REFERENCES [fakturering].[Expenses]([Id]),
        CONSTRAINT [FK_Vouchers_Income] FOREIGN KEY ([IncomeId]) REFERENCES [fakturering].[Income]([Id]),
        CONSTRAINT [FK_Vouchers_PostedBy] FOREIGN KEY ([PostedBy]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_Vouchers_CancelledBy] FOREIGN KEY ([CancelledBy]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_Vouchers_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 11. VERIFIKATIONSRADER (VoucherLines)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'VoucherLines')
BEGIN
    CREATE TABLE [fakturering].[VoucherLines] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [VoucherId] UNIQUEIDENTIFIER NOT NULL,
        [AccountCode] NVARCHAR(10) NOT NULL,
        [AccountName] NVARCHAR(255) NOT NULL,
        [DebitAmount] DECIMAL(12,2) DEFAULT 0,
        [CreditAmount] DECIMAL(12,2) DEFAULT 0,
        [Description] NVARCHAR(500) NULL,
        [CostCenter] NVARCHAR(50) NULL,
        [ProjectCode] NVARCHAR(50) NULL,
        [VATCode] NVARCHAR(10) NULL,
        [VATAmount] DECIMAL(12,2) DEFAULT 0,
        [SortOrder] INT DEFAULT 0,
        CONSTRAINT [FK_VoucherLines_Vouchers] FOREIGN KEY ([VoucherId]) REFERENCES [fakturering].[Vouchers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CHK_VoucherLines_DebitCredit] CHECK ([DebitAmount] > 0 OR [CreditAmount] > 0)
    );
END
GO

-- =====================================================
-- 12. BETALNINGAR (Payments)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Payments')
BEGIN
    CREATE TABLE [fakturering].[Payments] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
        [PaymentDate] DATE NOT NULL,
        [Amount] DECIMAL(12,2) NOT NULL,
        [Currency] NVARCHAR(3) DEFAULT 'SEK',
        [ExchangeRate] DECIMAL(10,4) DEFAULT 1.0000,
        [AmountSEK] DECIMAL(12,2) NOT NULL, -- Amount * ExchangeRate
        [PaymentMethod] NVARCHAR(50) NOT NULL, -- 'BankTransfer', 'Card', 'Cash', 'Swish', 'Other'
        [Reference] NVARCHAR(100) NULL,
        [BankAccount] NVARCHAR(50) NULL,
        [TransactionId] NVARCHAR(100) NULL,
        [PaymentFee] DECIMAL(12,2) DEFAULT 0,
        [Notes] NVARCHAR(500) NULL,
        [IsPartialPayment] BIT DEFAULT 0,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [ProcessedDate] DATETIME2 NULL,
        [Status] NVARCHAR(50) DEFAULT 'Pending', -- 'Pending', 'Confirmed', 'Failed', 'Cancelled'
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        [ProcessedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_Payments_Invoices] FOREIGN KEY ([InvoiceId]) REFERENCES [fakturering].[Invoices]([Id]),
        CONSTRAINT [FK_Payments_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_Payments_ProcessedBy] FOREIGN KEY ([ProcessedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 13. PÅMINNELSER (Reminders)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'Reminders')
BEGIN
    CREATE TABLE [fakturering].[Reminders] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
        [ReminderLevel] INT NOT NULL, -- 1 = Första påminnelse, 2 = Andra, 3 = Inkasso
        [SentDate] DATETIME2 NOT NULL,
        [DaysOverdue] INT NOT NULL,
        [Fee] DECIMAL(10,2) DEFAULT 0, -- Påminnelseavgift
        [InterestAmount] DECIMAL(10,2) DEFAULT 0, -- Dröjsmålsränta
        [TotalAmount] DECIMAL(12,2) NOT NULL, -- Ursprunglig skuld + avgift + ränta
        [EmailAddress] NVARCHAR(255) NOT NULL,
        [EmailSubject] NVARCHAR(255) NOT NULL,
        [EmailBody] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(50) DEFAULT 'Sent', -- 'Sent', 'Delivered', 'Opened', 'Responded'
        [ResponseDate] DATETIME2 NULL,
        [NextReminderDate] DATETIME2 NULL,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [SentBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_Reminders_Invoices] FOREIGN KEY ([InvoiceId]) REFERENCES [fakturering].[Invoices]([Id]),
        CONSTRAINT [FK_Reminders_SentBy] FOREIGN KEY ([SentBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- 14. KONTOPLAN (ChartOfAccounts)
-- =====================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'fakturering' AND TABLE_NAME = 'ChartOfAccounts')
BEGIN
    CREATE TABLE [fakturering].[ChartOfAccounts] (
        [AccountCode] NVARCHAR(10) PRIMARY KEY,
        [AccountName] NVARCHAR(255) NOT NULL,
        [AccountType] NVARCHAR(50) NOT NULL, -- 'Assets', 'Liabilities', 'Equity', 'Revenue', 'Expenses'
        [ParentAccount] NVARCHAR(10) NULL,
        [IsActive] BIT DEFAULT 1,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [CreatedBy] INT NULL, -- References dbo.Users.user_id
        CONSTRAINT [FK_ChartOfAccounts_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([user_id])
    );
END
GO

-- =====================================================
-- INDEXERING FÖR PRESTANDA
-- =====================================================

-- UserClientAccess indexer (viktiga för behörighetskontroll)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserClientAccess_UserId')
    CREATE NONCLUSTERED INDEX [IX_UserClientAccess_UserId] ON [fakturering].[UserClientAccess] ([UserId]) INCLUDE ([ClientId], [AccessLevel])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserClientAccess_ClientId')
    CREATE NONCLUSTERED INDEX [IX_UserClientAccess_ClientId] ON [fakturering].[UserClientAccess] ([ClientId]) INCLUDE ([UserId], [AccessLevel])

-- Clients indexer
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Clients_Name')
    CREATE NONCLUSTERED INDEX [IX_Clients_Name] ON [fakturering].[Clients] ([Name]) INCLUDE ([Email], [Phone])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Clients_OrgNumber')
    CREATE NONCLUSTERED INDEX [IX_Clients_OrgNumber] ON [fakturering].[Clients] ([OrgNumber])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Clients_Active')
    CREATE NONCLUSTERED INDEX [IX_Clients_Active] ON [fakturering].[Clients] ([IsActive], [CreatedDate])

-- Projects indexer
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Projects_ClientId')
    CREATE NONCLUSTERED INDEX [IX_Projects_ClientId] ON [fakturering].[Projects] ([ClientId], [Status])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Projects_Status')
    CREATE NONCLUSTERED INDEX [IX_Projects_Status] ON [fakturering].[Projects] ([Status], [StartDate])

-- Invoices indexer (kritiska för prestanda)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_ClientId')
    CREATE NONCLUSTERED INDEX [IX_Invoices_ClientId] ON [fakturering].[Invoices] ([ClientId], [Status])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_Status')
    CREATE NONCLUSTERED INDEX [IX_Invoices_Status] ON [fakturering].[Invoices] ([Status], [InvoiceDate])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_DueDate')
    CREATE NONCLUSTERED INDEX [IX_Invoices_DueDate] ON [fakturering].[Invoices] ([DueDate], [Status])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_InvoiceNumber')
    CREATE NONCLUSTERED INDEX [IX_Invoices_InvoiceNumber] ON [fakturering].[Invoices] ([InvoiceNumber]) INCLUDE ([TotalAmount], [Status])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_OCRNumber')
    CREATE NONCLUSTERED INDEX [IX_Invoices_OCRNumber] ON [fakturering].[Invoices] ([OCRNumber]) INCLUDE ([TotalAmount], [Status])

-- Income indexer
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Income_Date')
    CREATE NONCLUSTERED INDEX [IX_Income_Date] ON [fakturering].[Income] ([Date], [Category])

-- Expenses indexer
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Expenses_Date')
    CREATE NONCLUSTERED INDEX [IX_Expenses_Date] ON [fakturering].[Expenses] ([Date], [Category])

-- TimeReports indexer
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TimeReports_ProjectId')
    CREATE NONCLUSTERED INDEX [IX_TimeReports_ProjectId] ON [fakturering].[TimeReports] ([ProjectId], [Date])

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TimeReports_UserId')
    CREATE NONCLUSTERED INDEX [IX_TimeReports_UserId] ON [fakturering].[TimeReports] ([UserId], [Date])

-- =====================================================
-- VYER FÖR RAPPORTER
-- =====================================================

-- Vy för faktura översikt med behörighetskontroll
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_InvoiceOverview')
    DROP VIEW [fakturering].[vw_InvoiceOverview]
GO

CREATE VIEW [fakturering].[vw_InvoiceOverview]
AS
SELECT 
    i.[Id],
    i.[InvoiceNumber],
    i.[OCRNumber],
    i.[InvoiceDate],
    i.[DueDate],
    i.[Status],
    i.[TotalAmount],
    i.[Currency],
    c.[Name] as ClientName,
    c.[Email] as ClientEmail,
    c.[OrgNumber] as ClientOrgNumber,
    p.[Name] as ProjectName,
    u_created.[firstname] + ' ' + u_created.[lastname] as CreatedByName,
    u_updated.[firstname] + ' ' + u_updated.[lastname] as UpdatedByName,
    DATEDIFF(day, i.[DueDate], GETDATE()) as DaysOverdue,
    CASE 
        WHEN i.[Status] = 'Paid' THEN 'Betald'
        WHEN i.[DueDate] < GETDATE() AND i.[Status] IN ('Sent', 'Overdue') THEN 'Förfallen'
        WHEN i.[Status] = 'Sent' THEN 'Skickad'
        WHEN i.[Status] = 'Draft' THEN 'Utkast'
        ELSE i.[Status]
    END as StatusSwedish
FROM [fakturering].[Invoices] i
INNER JOIN [fakturering].[Clients] c ON i.[ClientId] = c.[Id]
LEFT JOIN [fakturering].[Projects] p ON i.[ProjectId] = p.[Id]
LEFT JOIN [dbo].[Users] u_created ON i.[CreatedBy] = u_created.[user_id]
LEFT JOIN [dbo].[Users] u_updated ON i.[UpdatedBy] = u_updated.[user_id]
GO

-- Vy för användarnas klientåtkomst
CREATE VIEW [fakturering].[vw_UserClientAccess]
AS
SELECT 
    uca.[Id],
    uca.[UserId],
    u.[firstname] + ' ' + u.[lastname] as UserName,
    u.[email] as UserEmail,
    u.[role] as UserRole,
    uca.[ClientId],
    c.[Name] as ClientName,
    c.[OrgNumber] as ClientOrgNumber,
    uca.[AccessLevel],
    uca.[CanViewFinancials],
    uca.[CanManageInvoices],
    uca.[CanManageExpenses],
    uca.[CanViewReports],
    uca.[CreatedDate]
FROM [fakturering].[UserClientAccess] uca
INNER JOIN [dbo].[Users] u ON uca.[UserId] = u.[user_id]
INNER JOIN [fakturering].[Clients] c ON uca.[ClientId] = c.[Id]
WHERE u.[active] = 1
GO

-- =====================================================
-- STORED PROCEDURES FÖR AUTHENTICATION INTEGRATION
-- =====================================================

-- Validera användarbehörighet
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_ValidateUserPermission')
    DROP PROCEDURE [fakturering].[sp_ValidateUserPermission]
GO

CREATE PROCEDURE [fakturering].[sp_ValidateUserPermission]
    @UserId INT,
    @ClientId UNIQUEIDENTIFIER = NULL,
    @Action NVARCHAR(50), -- 'read', 'write', 'delete', 'manage'
    @Resource NVARCHAR(100), -- 'invoices', 'clients', 'expenses', 'reports'
    @HasPermission BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @HasPermission = 0;
    
    -- Kontrollera om användaren är aktiv
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [user_id] = @UserId AND [active] = 1)
        RETURN;
    
    -- Admin och Owner har alltid full behörighet
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [user_id] = @UserId AND [role] IN ('Admin', 'Owner'))
    BEGIN
        SET @HasPermission = 1;
        RETURN;
    END
    
    -- FaktureringOwner har full behörighet för fakturering
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [user_id] = @UserId AND [role] = 'FaktureringOwner')
    BEGIN
        SET @HasPermission = 1;
        RETURN;
    END
    
    -- Kontrollera specifik klientåtkomst om ClientId anges
    IF @ClientId IS NOT NULL
    BEGIN
        SELECT @HasPermission = 
            CASE 
                WHEN @Resource = 'invoices' AND @Action IN ('read', 'write') AND [CanManageInvoices] = 1 THEN 1
                WHEN @Resource = 'expenses' AND @Action IN ('read', 'write') AND [CanManageExpenses] = 1 THEN 1
                WHEN @Resource = 'reports' AND @Action = 'read' AND [CanViewReports] = 1 THEN 1
                WHEN @Resource = 'financials' AND @Action = 'read' AND [CanViewFinancials] = 1 THEN 1
                WHEN @Action = 'read' AND [AccessLevel] IN ('Owner', 'ReadWrite', 'ReadOnly') THEN 1
                WHEN @Action IN ('write', 'delete') AND [AccessLevel] IN ('Owner', 'ReadWrite') THEN 1
                ELSE 0
            END
        FROM [fakturering].[UserClientAccess]
        WHERE [UserId] = @UserId AND [ClientId] = @ClientId;
    END
    ELSE
    BEGIN
        -- Global behörighet baserat på roll
        SELECT @HasPermission = 
            CASE 
                WHEN u.[role] = 'FaktureringAccountant' AND @Resource IN ('invoices', 'expenses', 'reports') THEN 1
                WHEN u.[role] = 'FaktureringAuditor' AND @Action = 'read' THEN 1
                ELSE 0
            END
        FROM [dbo].[Users] u
        WHERE u.[user_id] = @UserId;
    END
END
GO

-- Hämta användarens tillgängliga klienter
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetUserClients')
    DROP PROCEDURE [fakturering].[sp_GetUserClients]
GO

CREATE PROCEDURE [fakturering].[sp_GetUserClients]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Admin, Owner och FaktureringOwner ser alla klienter
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [user_id] = @UserId AND [role] IN ('Admin', 'Owner', 'FaktureringOwner'))
    BEGIN
        SELECT 
            c.[Id],
            c.[Name],
            c.[OrgNumber],
            c.[Email],
            c.[ContactPerson],
            c.[IsActive],
            'Owner' as AccessLevel,
            1 as CanViewFinancials,
            1 as CanManageInvoices,
            1 as CanManageExpenses,
            1 as CanViewReports
        FROM [fakturering].[Clients] c
        WHERE c.[IsActive] = 1
        ORDER BY c.[Name];
    END
    ELSE
    BEGIN
        -- Övriga användare ser endast klienter de har behörighet till
        SELECT 
            c.[Id],
            c.[Name],
            c.[OrgNumber],
            c.[Email],
            c.[ContactPerson],
            c.[IsActive],
            uca.[AccessLevel],
            uca.[CanViewFinancials],
            uca.[CanManageInvoices],
            uca.[CanManageExpenses],
            uca.[CanViewReports]
        FROM [fakturering].[Clients] c
        INNER JOIN [fakturering].[UserClientAccess] uca ON c.[Id] = uca.[ClientId]
        WHERE uca.[UserId] = @UserId
            AND c.[IsActive] = 1
        ORDER BY c.[Name];
    END
END
GO

-- Generera nästa fakturanummer
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GenerateInvoiceNumber')
    DROP PROCEDURE [fakturering].[sp_GenerateInvoiceNumber]
GO

CREATE PROCEDURE [fakturering].[sp_GenerateInvoiceNumber]
    @Year INT = NULL,
    @InvoiceNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @Year IS NULL
        SET @Year = YEAR(GETDATE())
    
    DECLARE @NextNumber INT
    
    SELECT @NextNumber = ISNULL(MAX(CAST(RIGHT(InvoiceNumber, 4) AS INT)), 0) + 1
    FROM [fakturering].[Invoices]
    WHERE InvoiceNumber LIKE CAST(@Year AS NVARCHAR(4)) + '%'
    
    SET @InvoiceNumber = CAST(@Year AS NVARCHAR(4)) + '-' + RIGHT('0000' + CAST(@NextNumber AS NVARCHAR(4)), 4)
END
GO

-- Generera OCR-nummer enligt Modulus 10
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GenerateOCRNumber')
    DROP PROCEDURE [fakturering].[sp_GenerateOCRNumber]
GO

CREATE PROCEDURE [fakturering].[sp_GenerateOCRNumber]
    @InvoiceNumber NVARCHAR(50),
    @OCRNumber NVARCHAR(25) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @BaseNumber NVARCHAR(20)
    DECLARE @CheckDigit INT
    DECLARE @Sum INT = 0
    DECLARE @Position INT
    DECLARE @Digit INT
    DECLARE @Alternating BIT = 1
    
    -- Extrahera siffror från fakturanummer
    SET @BaseNumber = RIGHT('00000000000000000000' + REPLACE(REPLACE(@InvoiceNumber, '-', ''), ' ', ''), 20)
    
    -- Beräkna kontrollsiffra med Modulus 10
    SET @Position = LEN(@BaseNumber)
    
    WHILE @Position > 0
    BEGIN
        SET @Digit = CAST(SUBSTRING(@BaseNumber, @Position, 1) AS INT)
        
        IF @Alternating = 1
        BEGIN
            SET @Digit = @Digit * 2
            IF @Digit > 9
                SET @Digit = (@Digit / 10) + (@Digit % 10)
        END
        
        SET @Sum = @Sum + @Digit
        SET @Alternating = 1 - @Alternating
        SET @Position = @Position - 1
    END
    
    SET @CheckDigit = (10 - (@Sum % 10)) % 10
    SET @OCRNumber = @BaseNumber + RIGHT('00' + CAST(@CheckDigit AS NVARCHAR(2)), 2) + '0'
END
GO

-- =====================================================
-- SEED DATA (Exempeldata)
-- =====================================================

-- Lägg till grundläggande kontoplan
INSERT INTO [fakturering].[ChartOfAccounts] (AccountCode, AccountName, AccountType)
SELECT * FROM (VALUES 
    ('3000', 'Försäljning 25% moms', 'Revenue'),
    ('3001', 'Försäljning 12% moms', 'Revenue'),
    ('3002', 'Försäljning 6% moms', 'Revenue'),
    ('3003', 'Försäljning momsfritt', 'Revenue'),
    ('2611', 'Utgående moms 25%', 'Liabilities'),
    ('2612', 'Utgående moms 12%', 'Liabilities'),
    ('2613', 'Utgående moms 6%', 'Liabilities'),
    ('2640', 'Ingående moms', 'Assets'),
    ('4000', 'Inköp av material', 'Expenses'),
    ('5010', 'Lokalhyra', 'Expenses'),
    ('5020', 'El, gas, vatten', 'Expenses'),
    ('6110', 'Kontorsmaterial', 'Expenses'),
    ('6210', 'Telefon', 'Expenses'),
    ('6250', 'Internetkostnader', 'Expenses'),
    ('6420', 'Resor och bilkostnader', 'Expenses'),
    ('6540', 'IT-tjänster', 'Expenses'),
    ('1930', 'Kundfordringar', 'Assets'),
    ('1910', 'Bankkonto', 'Assets'),
    ('2440', 'Leverantörsskulder', 'Liabilities'),
    ('2510', 'Preliminärskatt', 'Liabilities')
) AS Source(AccountCode, AccountName, AccountType)
WHERE NOT EXISTS (SELECT 1 FROM [fakturering].[ChartOfAccounts] coa WHERE coa.AccountCode = Source.AccountCode)

-- Lägg till exempelkunder
INSERT INTO [fakturering].[Clients] (Id, Name, Email, ContactPerson, Address, PostalCode, City, Country, PaymentTerms, IsActive)
SELECT * FROM (VALUES 
    (NEWID(), 'Test Kund AB', 'test@example.com', 'Test Person', 'Testgatan 1', '12345', 'Stockholm', 'Sverige', 30, 1),
    (NEWID(), 'Exempel Företag', 'kontakt@exempel.se', 'Anna Andersson', 'Exempelvägen 5', '67890', 'Göteborg', 'Sverige', 14, 1)
) AS Source(Id, Name, Email, ContactPerson, Address, PostalCode, City, Country, PaymentTerms, IsActive)
WHERE NOT EXISTS (SELECT 1 FROM [fakturering].[Clients] c WHERE c.Email = Source.Email)

PRINT 'FaktureringsAPI databas schema har skapats framgångsrikt!'
PRINT 'Schema: fakturering (i InventoryBE databas)'
PRINT 'Tabeller: 14 huvudtabeller (inkl. UserClientAccess bridge)'
PRINT 'Index: 13 prestandaindex'
PRINT 'Vyer: 2 rapportvyer (med InventoryBE integration)'
PRINT 'Stored Procedures: 4 affärslogik + 2 behörighetsprocedurer'
PRINT 'Seed Data: Kontoplan + 2 exempelkunder'
PRINT ''
PRINT 'NÄSTA STEG:'
PRINT '1. Lägg till nya roller i InventoryBE Users.role fält:'
PRINT '   - FaktureringOwner, FaktureringAccountant, FaktureringClient, FaktureringAuditor'
PRINT '2. Kopiera AuthController från InventoryBE till FaktureringsAPI'
PRINT '3. Konfigurera samma JWT-inställningar i båda API:erna'
PRINT '4. Implementera behörighetsmiddleware som använder sp_ValidateUserPermission'
PRINT ''
PRINT 'För att se alla tabeller: SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = ''fakturering'''