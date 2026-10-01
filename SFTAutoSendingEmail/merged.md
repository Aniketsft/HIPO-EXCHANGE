# HipoExchange Developer Guide
## Table of Contents
1. [Core Syntax](#core-syntax)
2. [Required System Tags](#required-system-tags)
3. [Optional System Tags](#optional-system-tags)
4. [Dynamic Variables (Custom Tags)](#dynamic-variables-custom-tags)
5. [Scenario Examples](#scenario-examples)
6. [Multi-Grouping (Batching Multiple Invoices for One Customer)](#5-multi-grouping-batching-multiple-invoices-for-one-customer)
7. [Best Practices for Sage X3 / Crystal Reports Developers](#best-practices-for-sage-x3--crystal-reports-developers)
8. [Crystal Reports: Formula Guide & Scenarios](#crystal-reports-formula-guide--scenarios)

# HipoExchange Metadata Specification

This document provides a comprehensive guide for injecting metadata tags into Crystal Reports (from Sage X3) for **HipoExchange** to process.

By rendering these hidden strings in your PDF reports (usually in white 1pt font), HipoExchange will instantly understand how to split, encrypt, and email the documents.

## Core Syntax

Every document generated must contain a metadata string formatted as follows:

```text
@@HIPO@@|TYPE:[ProfileID]|EMAIL:[Recipient]|PASSWORD:[Optional]|VAR1:[Value1]|VAR2:[Value2]@@ENDHIPO@@
```

### Required System Tags

| Tag | Description | Example |
|---|---|---|
| `@@HIPO@@` | The opening tag that HipoExchange looks for. | `@@HIPO@@` |
| `TYPE:` | The Profile ID configured in the HipoExchange UI. Dictates the SMTP server and HTML template used. | `TYPE:INV` |
| `EMAIL:` | The destination email address. Multiple emails are not supported here (use CC/BCC in UI instead). | `EMAIL:finance@client.com` |
| `@@ENDHIPO@@`| The closing tag. | `@@ENDHIPO@@` |

### Optional System Tags

| Tag | Description | Example |
|---|---|---|
| `PASSWORD:` | If the profile has **"Enable PDF Encryption"** checked, HipoExchange will use this value to AES-encrypt the attached PDF. Highly recommended for sensitive data. | `PASSWORD:12345` |

### Dynamic Variables (Custom Tags)

Any additional tags you append (e.g., `INVOICE_NO:`, `BALANCE:`, `BUYER:`) will be parsed as dynamic variables. 
In your HipoExchange HTML templates, you can inject these variables using curly braces (e.g., `Hello, your balance is {BALANCE}`).

---

## Scenario Examples

### 1. Invoices (INV)
**Scenario:** Sending a standard invoice. It requires no password, but uses dynamic variables to populate the email body.

**Crystal Report String:**
```text
@@HIPO@@|TYPE:INV|EMAIL:billing@customer.com|INVOICE_NO:INV-2024-991|TOTAL:$4,500.00|DUE_DATE:Oct 31, 2024@@ENDHIPO@@
```

**HipoExchange HTML Template Example:**
```html
<h2>New Invoice Available</h2>
<p>Your invoice <strong>{INVOICE_NO}</strong> for the amount of <strong>{TOTAL}</strong> is attached.</p>
<p>Please ensure payment is made by {DUE_DATE}.</p>
```

### 2. Statement of Account (SOA)
**Scenario:** Sending a sensitive financial statement. The PDF must be password protected using a customer-specific code (e.g., their Customer ID or Tax ID).

**Crystal Report String:**
```text
@@HIPO@@|TYPE:SOA|EMAIL:accounts@partner.com|PASSWORD:CUST-8832|CUSTOMER_NAME:Acme Corp|BALANCE:$12,450.00@@ENDHIPO@@
```

**HipoExchange HTML Template Example:**
```html
<h2>Monthly Statement of Account</h2>
<p>Dear {CUSTOMER_NAME},</p>
<p>Your current outstanding balance is <strong>{BALANCE}</strong>. A detailed statement is attached to this email.</p>
<p><em>Note: The attached PDF is encrypted. The password is your Customer ID.</em></p>
```

### 3. Purchase Orders (PO)
**Scenario:** Automatically dispatching Purchase Orders to vendors.

**Crystal Report String:**
```text
@@HIPO@@|TYPE:PO|EMAIL:orders@supplier.com|PO_NUM:PO-44219|BUYER_NAME:John Doe|URGENCY:High@@ENDHIPO@@
```

**HipoExchange HTML Template Example:**
```html
<h2>Purchase Order: {PO_NUM}</h2>
<p>Please find our latest purchase order attached.</p>
<p>Buyer: {BUYER_NAME}</p>
<p>Urgency: <strong>{URGENCY}</strong></p>
```

### 4. Employee Payslips (PAY)
**Scenario:** Delivering highly sensitive payroll documents to employees. Must be strictly encrypted, often using the employee's Date of Birth (e.g., YYYYMMDD).

**Crystal Report String:**
```text
@@HIPO@@|TYPE:PAY|EMAIL:j.smith@company.com|PASSWORD:19900101|EMP_NAME:Jane Smith|PERIOD:October 2024@@ENDHIPO@@
```

**HipoExchange HTML Template Example:**
```html
<h2>Payslip - {PERIOD}</h2>
<p>Hi {EMP_NAME},</p>
<p>Your payslip for {PERIOD} is attached. Please use your date of birth (YYYYMMDD) to unlock the document.</p>
```

---

## Best Practices for Sage X3 / Crystal Reports Developers

1. **Color & Size:** Place this formula field at the very top or bottom of the report footer/header. Set the font color to White (`#FFFFFF`) and the font size to `1pt` so it is invisible to the human eye but easily parsable by HipoExchange.
2. **Page Grouping:** When generating a massive batch PDF (e.g., 500 invoices in one file), ensure the `@@HIPO@@` tag appears on **Page 1** of every individual invoice. HipoExchange splits the batch whenever it detects a new `@@HIPO@@` tag.
3. **No Line Breaks:** Ensure the Crystal Reports formula does not inject accidental line breaks or carriage returns inside the `@@HIPO@@...@@ENDHIPO@@` string.
4. **Delimiter Safety:** Do not use the pipe `|` character in your dynamic variable values, as it is the system delimiter.
# Crystal Reports: Formula Guide & Scenarios

This guide provides exactly what your Sage X3 developers need to write the Crystal Reports formulas for every possible dispatch scenario in HipoExchange. 

All formulas should be placed in a Formula Field and dropped onto **Page 1** (or every page) of the relevant report section, formatted with **White Text (`#FFFFFF`)** at **1pt Size**.

---

### Scenario 1: The Standard Dispatch (1 Invoice = 1 Email)
**Use Case:** The most common scenario. You generate a massive batch of 500 invoices, and you want each invoice sent to its respective customer.
**Crystal Syntax:**
```crystalreports
"@@HIPO@@|TYPE:INV|EMAIL:" + {Customer.EmailAddress} + "|FILENAME:" + {Invoice.InvoiceNo} + "@@ENDHIPO@@"
```
* **Required Outcome:** HipoExchange detects 500 different `FILENAME` tags, physically splits the giant PDF into 500 mini-PDFs, and emails each one individually.

---

### Scenario 2: Multi-Grouping Option A (Stitched / Merged PDF)
**Use Case:** You are generating a month-end Statement of Account. The statement spans 5 pages for Customer A. You want all 5 pages merged into a **single PDF file** attached to 1 email.
**Crystal Syntax:**
```crystalreports
"@@HIPO@@|TYPE:SOA|EMAIL:" + {Customer.EmailAddress} + "|GROUP:" + {Customer.CustomerID} + "@@ENDHIPO@@"
```
* **Required Outcome:** Because you used the `GROUP` tag (and it remains identical across all 5 pages for Customer A), HipoExchange will stitch all 5 pages together into one massive PDF. It sends exactly 1 email containing 1 attachment.

---

### Scenario 3: Multi-Grouping Option B (Separate PDF Attachments)
**Use Case:** Customer A has 5 separate invoices. They hate merged PDFs. They want **1 email** containing **5 separate PDF attachments** (`INV-1.pdf`, `INV-2.pdf`, etc.).
**Crystal Syntax:**
```crystalreports
"@@HIPO@@|TYPE:INV|EMAIL:" + {Customer.EmailAddress} + "|FILENAME:" + {Invoice.InvoiceNo} + "@@ENDHIPO@@"
```
* **Required Outcome:** Notice how this is the exact same formula as Scenario 1! HipoExchange splits the batch into 5 separate PDFs because the `FILENAME` changes. However, the dispatcher engine automatically notices that all 5 files are destined for the exact same `{Customer.EmailAddress}`, so it logically groups them and sends **1 email** containing all 5 attachments.

---

### Scenario 4: Encrypted High-Security Documents
**Use Case:** Distributing HR payslips. Every PDF must be heavily AES-encrypted so that only the employee can open it using their Date of Birth (YYYYMMDD).
*(Ensure "Enable PDF Encryption" is checked for the PAY profile in HipoExchange UI).*
**Crystal Syntax:**
```crystalreports
"@@HIPO@@|TYPE:PAY|EMAIL:" + {Employee.EmailAddress} + "|FILENAME:" + {Payroll.SlipID} + "|PASSWORD:" + ToText({Employee.DateOfBirth}, "yyyyMMdd") + "@@ENDHIPO@@"
```
* **Required Outcome:** HipoExchange intercepts the file, applies military-grade AES encryption using the specific employee's DOB, and emails it. If the email is intercepted by a third party, the PDF cannot be opened.

---

### Scenario 5: Dynamic Mail Merge (Custom Email Bodies)
**Use Case:** You don't want to send a boring, generic email. You want the email text to say: *"Dear John, your balance of $5,000 is due on Friday."*
**Crystal Syntax:**
```crystalreports
"@@HIPO@@|TYPE:INV|EMAIL:" + {Customer.EmailAddress} + "|FILENAME:" + {Invoice.InvoiceNo} + "|FIRST_NAME:" + {Customer.FirstName} + "|BALANCE:" + ToText({Invoice.TotalAmount}) + "@@ENDHIPO@@"
```
* **Required Outcome:** Any tag that isn't a system keyword (like `FIRST_NAME:` or `BALANCE:`) is instantly captured as a dynamic variable. Over in the HipoExchange UI, you simply type `{FIRST_NAME}` and `{BALANCE}` in the HTML Template box, and the engine will dynamically swap out the text for every single email!
