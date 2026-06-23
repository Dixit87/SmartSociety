USE SmartSociety;
GO

-- 1. SP: sp_Expense_Update
CREATE OR ALTER PROCEDURE sp_Expense_Update
    @ExpenseId INT,
    @CategoryId INT,
    @Title VARCHAR(150),
    @Amount DECIMAL(18,2),
    @DateIncurred DATE,
    @PaidTo VARCHAR(100),
    @PaymentMethod VARCHAR(50),
    @ReferenceNo VARCHAR(100) = NULL,
    @Notes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Expenses
    SET CategoryId = @CategoryId,
        Title = @Title,
        Amount = @Amount,
        DateIncurred = @DateIncurred,
        PaidTo = @PaidTo,
        PaymentMethod = @PaymentMethod,
        ReferenceNo = @ReferenceNo,
        Notes = @Notes
    WHERE ExpenseId = @ExpenseId;
END
GO

-- 2. SP: sp_Income_Update
CREATE OR ALTER PROCEDURE sp_Income_Update
    @IncomeId INT,
    @Source VARCHAR(100),
    @Title VARCHAR(150),
    @Amount DECIMAL(18,2),
    @DateReceived DATE,
    @ReceivedFrom VARCHAR(100),
    @PaymentMethod VARCHAR(50),
    @ReferenceNo VARCHAR(100) = NULL,
    @Notes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE OtherIncomes
    SET Source = @Source,
        Title = @Title,
        Amount = @Amount,
        DateReceived = @DateReceived,
        ReceivedFrom = @ReceivedFrom,
        PaymentMethod = @PaymentMethod,
        ReferenceNo = @ReferenceNo,
        Notes = @Notes
    WHERE IncomeId = @IncomeId;
END
GO
