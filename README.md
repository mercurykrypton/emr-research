# EMR_Research

Patient search tool for Intrahealth Profile EMR - search by diagnosis, billing code, DOB, and other criteria.

## Database Access Findings

### Direct Firebird Access
- **Blocked.** Profile's database is cloud-hosted via `018.ppn.ihhost.net:443` (Intrahealth's servers).
- Port 3050 (Firebird default) is closed on the local network.
- Connections use RPC (`ncacn_ip_tcp`) remapped to HTTPS - no direct DB client access possible without Intrahealth cooperation.

### ISProfile Macro API (Primary Access Method)
Macros run inside `Profile.exe` with full data access via the `ISProfile` in-process COM object.

**Confirmed working patient properties:**
- `LoadPatient(id)` - iterate all patients by numeric internal ID (IDs sparse up to ~2M)
- `FindPatientByNHI(phn)` - lookup by health card number
- `.SurName`, `.FirstName`, `.DOB`, `.NationalIdentifier`
- `.HomePhone`, `.MobilePhone`, `.WorkPhone`
- `.ID`, `.Status`, `.StatusIndex`, `.PatientType`
- `.DateFirstSeen`, `.EnrolledDate`, `.CreatedDate`, `.LastModified`

**Unknown - requires probing:**
- `.Problems` / `.ProblemList` / `.Diagnoses` collections on patient object
- `LoadPatientProblems(ptntId)` or equivalent function
- `LoadSqlStatement(code)` - may allow running stored SQL reports

### Profile "Find Objects" (Built-in Query Engine)
- Access via: `Report > Find Objects`
- Can query patients by diagnosis code/description/concept, age/DOB, gender, registration date, billing, and more
- Queries can be saved as **Stored Queries** and run on demand
- Results can be exported to CSV/disk
- **Not yet automated** - currently GUI-only

### Profile "SQL Statements" (Potential Direct SQL)
- `Sqlstatement` CDO class exists in DataStructure.xml with field `SQLS_STATEMENT`
- Suggests Profile has a stored SQL report feature that executes raw SQL
- Output configurable to file (`SQLS_FILE_NAME`, `SQLS_OUTPUT_TO`)
- **Access path unknown** - may be accessible via macro API or a dedicated menu

## Database Schema (Key Tables)

All tables are Firebird SQL. Table/field names confirmed from `Profile\Bin\Web\DataStructure.xml`.

### PATIENT
| Field | Type | Description |
|-------|------|-------------|
| PTNT_ID | PK integer | Internal patient ID |
| PTNT_SURNAME | string | Last name |
| PTNT_FIRSTNAME | string | First name |
| PTNT_DOB | date | Date of birth |
| PTNT_NHI | string | Health card / PHN |

### PATIENTPROBLEM (Diagnosis/Problem List)
| Field | Type | Description |
|-------|------|-------------|
| PAPR_ID | PK integer | Record ID |
| PTNT_ID | FK -> PATIENT | Patient |
| PAPR_DXID | FK -> DISEASECODE | Disease code record |
| PAPR_DXDESCRIPTION | string | Diagnosis description |
| PAPR_DATE | date | Date of diagnosis |
| PAPR_TODATE | date | Resolved date |
| PAPR_STATUSID | integer | Status (active/resolved/etc.) |
| PAPR_TYPEID | integer | Type (problem/diagnosis/etc.) |
| PAPR_CONFIDENCEID | integer | Confidence (definitive/provisional/interim) |

### DISEASECODE
| Field | Type | Description |
|-------|------|-------------|
| DSCD_ID | PK integer | Record ID |
| DSCD_CODE | string | Code (Read code, custom, etc.) |
| DSCD_DESCRIPTION | string | Human-readable description |
| DSCD_OUTPUTCODE | string | ICD output code |

### ICD
| Field | Type | Description |
|-------|------|-------------|
| ICD_ID | PK integer | Record ID |
| ICD_IDENTIFIER | string | ICD code (e.g. H35.1) |
| ICD_DESCRIPTION | string | ICD description |

### TRANSACTIONS (Billing)
| Field | Type | Description |
|-------|------|-------------|
| TRNS_ID | PK integer | Transaction ID |
| PTNT_ID_SERVICE | FK -> PATIENT | Patient receiving service |
| TRNS_DATE | date | Transaction date |
| TRNS_TYPE | integer | Type |
| TRNS_AMOUNT | currency | Amount |

### TRANSACTIONDETAIL (Billing Diagnosis Codes)
| Field | Type | Description |
|-------|------|-------------|
| TRNS_ID | FK -> TRANSACTIONS | Parent transaction |
| DSCD_ID_1..4 | FK -> DISEASECODE | Up to 4 diagnosis codes on claim |
| TRDT_DESCRIPTION | string | Service description |

### TRANSACTIONLINE (Billing Line Items)
| Field | Type | Description |
|-------|------|-------------|
| TRLN_ID | PK integer | Line item ID |
| TRNS_ID | FK -> TRANSACTIONS | Parent transaction |
| LKLS_ID_AGENCY | FK -> PATIENT | Patient |

### DIAGNOSES_FOR_CASE
| Field | Type | Description |
|-------|------|-------------|
| OID | PK integer | Record ID |
| CASE_ID | FK -> BCASE | Case |
| DIAGNOSE_ID | FK -> DISEASECODE | Diagnosis |
| OPEN_DATE | date | Opened |
| CLOSED_DATE | date | Closed |
| PRINCIPAL | boolean | Principal diagnosis |

## Proposed Architecture

```
Phase 1: PROBE macro
  - Load a test patient and probe for .Problems/.Diagnoses/.Transactions collections
  - Test LoadSqlStatement() and related functions
  - Determine fastest path to diagnosis and billing data per patient

Phase 2: Export macros
  - PATIENT_DIAGNOSIS_EXPORT - dump all active patient diagnoses to CSV
  - PATIENT_BILLING_EXPORT   - dump transaction/billing codes to CSV
  - Incremental export support (track last export date)

Phase 3: WPF search tool (EMR_Research.exe)
  - Load exported CSVs into memory on startup
  - Search fields: Name, DOB, diagnosis code/description, billing code, date range
  - Double-click result -> open patient in Profile via macro
  - Refresh button -> re-run export macros and reload
```

## Next Steps

1. Write and run `PROBE_PATIENT_API.txt` macro in Profile to confirm diagnosis/billing API availability
2. Based on probe results, write export macros
3. Build WPF search UI

## Related Projects

- `P:\Merkur\Triage_OCR_AI` - existing macro examples (`PATIENTEXPORT.txt`, `FIXPATIENT.txt`, etc.)
- `P:\Fillable REQs\Gennix\Intrahealth\Profile\Bin\Web\DataStructure.xml` - full DB schema
- `P:\Fillable REQs\Gennix\Intrahealth\Profile\Bin\ProfileCAN.chm` - Profile help (Canadian variant)
