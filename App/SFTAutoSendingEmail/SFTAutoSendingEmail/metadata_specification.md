### 5. Multi-Grouping (Batching Multiple Invoices for One Customer)

HipoExchange has an extremely powerful built-in grouping engine. You have **two** different ways to bundle multiple documents for the same customer, depending on how they want to receive them.

#### Option A: One Email with ONE massive PDF (All invoices merged)
If a customer has 10 invoices, and you want to send them 1 email with 1 giant PDF attachment containing all 10 invoices.
*   **How to do it:** Use the GROUP: tag in your Crystal Report instead of (or in addition to) FILENAME:. Ensure every page for that customer has the exact same GROUP: value.
*   **Example String:** @@HIPO@@|TYPE:INV|EMAIL:finance@client.com|GROUP:CUST-883@@ENDHIPO@@
*   **Result:** HipoExchange will see that the GROUP hasn't changed across all 10 invoices. It will stitch all the pages together into one massive PDF and send it in 1 email.

#### Option B: One Email with MULTIPLE separate PDF attachments
If a customer has 10 invoices, and you want to send them 1 email, but you want it to have 10 separate PDF attachments (e.g., INV-1.pdf, INV-2.pdf, etc.).
*   **How to do it:** Give each invoice a unique FILENAME: tag, but use the exact same EMAIL: address.
*   **Example String Invoice 1:** @@HIPO@@|TYPE:INV|EMAIL:finance@client.com|FILENAME:INV-1@@ENDHIPO@@
*   **Example String Invoice 2:** @@HIPO@@|TYPE:INV|EMAIL:finance@client.com|FILENAME:INV-2@@ENDHIPO@@
*   **Result:** HipoExchange will split them into two physical files (INV-1.pdf and INV-2.pdf). Both files will enter the dispatch queue at the same time. The 10-second master daemon will detect that both PDFs are going to the same email address, and it will intelligently group them into **1 single email** containing both PDFs as separate attachments!
