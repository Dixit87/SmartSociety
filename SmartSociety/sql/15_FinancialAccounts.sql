USE SmartSociety;
GO

-- 1. Create ExpenseCategories Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseCategories')
BEGIN
    CREATE TABLE ExpenseCategories (
        CategoryId INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName VARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );

    -- Insert Default Categories
    INSERT INTO ExpenseCategories (CategoryName) VALUES 
    ('Staff Salaries'),
    ('Security Services'),
    ('Common Area Electricity'),
    ('Lift Maintenance'),
    ('Plumbing & Repairs'),
    ('Cleaning & Housekeeping'),
    ('Events & Celebrations'),
    ('Admin & Legal'),
    ('Taxes & Insurance'),
    ('Other Expenses');
END
GO

-- 2. Create Expenses Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
BEGIN
    CREATE TABLE Expenses (
        ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
        CategoryId INT NOT NULL,
        Title VARCHAR(150) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        DateIncurred DATE NOT NULL,
        PaidTo VARCHAR(100) NOT NULL,
        PaymentMethod VARCHAR(50) NOT NULL, -- Cash, Bank Transfer, Cheque, UPI
        ReferenceNo VARCHAR(100) NULL,
        ReceiptUrl VARCHAR(255) NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        
        FOREIGN KEY (CategoryId) REFERENCES ExpenseCategories(CategoryId)
    );
END
GO

-- 3. Create OtherIncomes Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OtherIncomes')
BEGIN
    CREATE TABLE OtherIncomes (
        IncomeId INT IDENTITY(1,1) PRIMARY KEY,
        Source VARCHAR(100) NOT NULL, -- e.g., Clubhouse Booking, Penalty, Move-in Charge
        Title VARCHAR(150) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        DateReceived DATE NOT NULL,
        ReceivedFrom VARCHAR(100) NOT NULL,
        PaymentMethod VARCHAR(50) NOT NULL,
        ReferenceNo VARCHAR(100) NULL,
        ReceiptUrl VARCHAR(255) NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 4. SP: sp_Expense_GetAll
CREATE OR ALTER PROCEDURE sp_Expense_GetAll
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.CategoryId,
        c.CategoryName,
        e.Title,
        e.Amount,
        e.DateIncurred,
        e.PaidTo,
        e.PaymentMethod,
        e.ReferenceNo,
        e.ReceiptUrl,
        e.Notes,
        e.CreatedAt
    FROM Expenses e
    INNER JOIN ExpenseCategories c ON e.CategoryId = c.CategoryId
    WHERE (@Month IS NULL OR MONTH(e.DateIncurred) = @Month)
      AND (@Year IS NULL OR YEAR(e.DateIncurred) = @Year)
    ORDER BY e.DateIncurred DESC, e.ExpenseId DESC;
END
GO

-- 5. SP: sp_Expense_Create
CREATE OR ALTER PROCEDURE sp_Expense_Create
    @CategoryId INT,
    @Title VARCHAR(150),
    @Amount DECIMAL(18,2),
    @DateIncurred DATE,
    @PaidTo VARCHAR(100),
    @PaymentMethod VARCHAR(50),
    @ReferenceNo VARCHAR(100) = NULL,
    @ReceiptUrl VARCHAR(255) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @ExpenseId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO Expenses (CategoryId, Title, Amount, DateIncurred, PaidTo, PaymentMethod, ReferenceNo, ReceiptUrl, Notes)
    VALUES (@CategoryId, @Title, @Amount, @DateIncurred, @PaidTo, @PaymentMethod, @ReferenceNo, @ReceiptUrl, @Notes);
    
    SET @ExpenseId = SCOPE_IDENTITY();
END
GO

-- 6. SP: sp_Expense_Delete
CREATE OR ALTER PROCEDURE sp_Expense_Delete
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Expenses WHERE ExpenseId = @ExpenseId;
END
GO

-- 7. SP: sp_Income_GetAll
CREATE OR ALTER PROCEDURE sp_Income_GetAll
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        IncomeId,
        Source,
        Title,
        Amount,
        DateReceived,
        ReceivedFrom,
        PaymentMethod,
        ReferenceNo,
        ReceiptUrl,
        Notes,
        CreatedAt
    FROM OtherIncomes
    WHERE (@Month IS NULL OR MONTH(DateReceived) = @Month)
      AND (@Year IS NULL OR YEAR(DateReceived) = @Year)
    ORDER BY DateReceived DESC, IncomeId DESC;
END
GO

-- 8. SP: sp_Income_Create
CREATE OR ALTER PROCEDURE sp_Income_Create
    @Source VARCHAR(100),
    @Title VARCHAR(150),
    @Amount DECIMAL(18,2),
    @DateReceived DATE,
    @ReceivedFrom VARCHAR(100),
    @PaymentMethod VARCHAR(50),
    @ReferenceNo VARCHAR(100) = NULL,
    @ReceiptUrl VARCHAR(255) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @IncomeId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO OtherIncomes (Source, Title, Amount, DateReceived, ReceivedFrom, PaymentMethod, ReferenceNo, ReceiptUrl, Notes)
    VALUES (@Source, @Title, @Amount, @DateReceived, @ReceivedFrom, @PaymentMethod, @ReferenceNo, @ReceiptUrl, @Notes);
    
    SET @IncomeId = SCOPE_IDENTITY();
END
GO

-- 9. SP: sp_Income_Delete
CREATE OR ALTER PROCEDURE sp_Income_Delete
    @IncomeId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM OtherIncomes WHERE IncomeId = @IncomeId;
END
GO

-- 10. SP: sp_Finance_GetDashboardStats
CREATE OR ALTER PROCEDURE sp_Finance_GetDashboardStats
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @TotalMaintenance DECIMAL(18,2) = 0;
    DECLARE @TotalUtility DECIMAL(18,2) = 0;
    DECLARE @TotalOtherIncome DECIMAL(18,2) = 0;
    DECLARE @TotalExpense DECIMAL(18,2) = 0;
    
    -- Maintenance Income (from BillPayments table)
    SELECT @TotalMaintenance = ISNULL(SUM(PaidAmount), 0)
    FROM BillPayments
    WHERE (@Month IS NULL OR MONTH(PaymentDate) = @Month)
      AND (@Year IS NULL OR YEAR(PaymentDate) = @Year);
      
    -- Utility Income (from UtilityPayments table)
    SELECT @TotalUtility = ISNULL(SUM(PaidAmount), 0)
    FROM UtilityPayments
    WHERE (@Month IS NULL OR MONTH(PaymentDate) = @Month)
      AND (@Year IS NULL OR YEAR(PaymentDate) = @Year);
      
    -- Other Income
    SELECT @TotalOtherIncome = ISNULL(SUM(Amount), 0)
    FROM OtherIncomes
    WHERE (@Month IS NULL OR MONTH(DateReceived) = @Month)
      AND (@Year IS NULL OR YEAR(DateReceived) = @Year);
      
    -- Total Expenses
    SELECT @TotalExpense = ISNULL(SUM(Amount), 0)
    FROM Expenses
    WHERE (@Month IS NULL OR MONTH(DateIncurred) = @Month)
      AND (@Year IS NULL OR YEAR(DateIncurred) = @Year);
      
    SELECT 
        @TotalMaintenance AS TotalMaintenanceIncome,
        @TotalUtility AS TotalUtilityIncome,
        @TotalOtherIncome AS TotalOtherIncome,
        (@TotalMaintenance + @TotalUtility + @TotalOtherIncome) AS TotalIncome,
        @TotalExpense AS TotalExpense,
        ((@TotalMaintenance + @TotalUtility + @TotalOtherIncome) - @TotalExpense) AS NetBalance;
END
GO

-- 11. SP: sp_Expense_GetChartData
CREATE OR ALTER PROCEDURE sp_Expense_GetChartData
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP 10
        c.CategoryName,
        SUM(e.Amount) AS TotalAmount
    FROM Expenses e
    INNER JOIN ExpenseCategories c ON e.CategoryId = c.CategoryId
    WHERE (@Month IS NULL OR MONTH(e.DateIncurred) = @Month)
      AND (@Year IS NULL OR YEAR(e.DateIncurred) = @Year)
    GROUP BY c.CategoryName
    ORDER BY TotalAmount DESC;
END
GO

-- 12. SP: sp_ExpenseCategory_GetAll
CREATE OR ALTER PROCEDURE sp_ExpenseCategory_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive 
    FROM ExpenseCategories 
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO
