﻿namespace NACHO
{
    public class BatchParser
    {
        public static Batch ParseHeader(string batchHeader, Batch batch)
        {
            string headerRecord = batchHeader.Substring(0, 1);
            string headerService = batchHeader.Substring(1, 3); 
            string headerCompanyName = batchHeader.Substring(4, 16);
            string companyDiscretionary = batchHeader.Substring(20, 20);
            string headerCompanyIdentification = batchHeader.Substring(40, 10);
            string standardEntry = batchHeader.Substring(50, 3); 
            string companyEntry = batchHeader.Substring(53, 10); 
            string companyDescriptionDate = batchHeader.Substring(63, 6); 
            string effectiveEntryDate = batchHeader.Substring(69, 6); 
            string settlementDate = batchHeader.Substring(75, 3); 
            string originatorStatus = batchHeader.Substring(78, 1); 
            string headerOriginatorDFI = batchHeader.Substring(79, 8); 
            string headerBatchNumber = batchHeader.Substring(87, 7); 

            batch.SetHeader(headerRecord,
                headerService,
                headerCompanyName,
                companyDiscretionary,
                headerCompanyIdentification,
                standardEntry,
                companyEntry,
                companyDescriptionDate,
                effectiveEntryDate,
                settlementDate,
                originatorStatus,
                headerOriginatorDFI,
                headerBatchNumber);

            return batch;
        }

        public static Batch ParseControl(string batchControl, Batch batch)
        {
            string controlRecordType            = batchControl.Substring(0, 1);
            string controlServiceClass          = batchControl.Substring(1, 3);
            string entryCount                   = batchControl.Substring(4, 6);
            string entryHash                    = batchControl.Substring(10, 10);
            string totalDebit                   = batchControl.Substring(20, 12);
            string totalCredit                  = batchControl.Substring(32, 12);
            string controlCompanyIdentification = batchControl.Substring(44, 10);
            string messageAuthentication        = batchControl.Substring(54, 19);
            string reserved                     = batchControl.Substring(73, 6);
            string controlOriginatorDFI         = batchControl.Substring(79, 8);
            string controlBatchNumber           = batchControl.Substring(87, 7);

            batch.SetControl(controlRecordType,
                controlServiceClass,
                entryCount,
                entryHash,
                totalDebit,
                totalCredit,
                controlCompanyIdentification,
                messageAuthentication,
                reserved,
                controlOriginatorDFI,
                controlBatchNumber);

            return batch;
        }       

        //todo handle current line in file passing and updating
        public static Batch ParseStream(System.IO.StreamReader reader, out string messages, out uint linesRead, uint currentLine)
        {
            linesRead = 0;
            messages = "";
            Batch batch = new Batch();
            int recordType;
            
            if ((recordType = reader.Peek()) != -1)
            {
                if (recordType != ACHParser.BATCH_HEADER_RECORD_TYPE_ASCII_VALUE)
                {
                    //consume line, message that not expected
                    string notBatchHeader = reader.ReadLine();
                    if (notBatchHeader != null)
                    {
                        messages += "\nDid not receive batch header as expected: " + notBatchHeader;
                        ++linesRead;
                    }
                    else
                    {
                        //TODO error message that its null
                    }
                }
                else
                {
                    //assume next line to consume is a batch header
                    //TODO check null?
                    string batchHeader = reader.ReadLine();
                    if (batchHeader != null)
                    {
                        ParseHeader(batchHeader, batch);                        
                        ++linesRead;

                        //TODO if batchheadermsg not null/ws then add to messages
                        //if not 94 chars then error message
                    }

                    bool expectingEntryType = true;//if true, then expecting entry, else expecting the last entry's addenda

                    //for testing just for the moment
                    //while not a batch control or end of file, consume a line
                    string devnull;
                    while ((recordType = reader.Peek()) != -1 
                        && recordType != ACHParser.BATCH_CONTROL_RECORD_TYPE_ASCII_VALUE 
                        && recordType != ACHParser.ACH_CONTROL_RECORD_TYPE_ASCII_VALUE)
                    {
                        //TODO more checks needed, must be an entry type, would be an error to get an ACH header here, or some random character
                        //if not an entry record type then error message

                        if (recordType == ACHParser.ENTRY_RECORD_TYPE_ASCII_VALUE)
                        {
                            if (!expectingEntryType)
                            {
                                //TODO log error that was expecting an addenda
                            }

                            //add entry (even if was expecting addenda)
                            Entry entry = EntryParser.ParseEntry(reader.ReadLine());

                            //TODO if entry msgs not null/ws then log
                            
                            batch.Entries.Add(entry);

                            ++linesRead;
                            //if entry says it has an addenda then set expecting entry to false
                            if (entry.AddendaRecord.Equals("1"))
                            {
                                expectingEntryType = false;
                            }
                        }
                        else if (recordType == ACHParser.ADDENDA_RECORD_TYPE_ASCII_VALUE)
                        {
                            if (expectingEntryType)
                            {
                                //TODO log error that wasn't expecting addenda
                            }

                            Addenda addenda = AddendaParser.ParseAddenda(reader.ReadLine());

                            //TODO if addendamsgs not null/ws, log

                            if (batch.Entries.Count <= 0)
                            {
                                //TODO log no entries to append this addenda
                            }
                            else
                            {
                                //TODO do a while addenda, can have many addenda  for each entry
                                ((Entry)batch.Entries[batch.Entries.Count - 1]).AddendaList.Add(addenda);
                            }

                            ++linesRead;

                            expectingEntryType = true;
                        }
                        else
                        {
                            //TODO log error that wasn't entry or addenda inside a batch
                        }
                        
                        
                        /*batch.Entries.Add(reader.ReadLine());

                        ++linesRead;*/
                        //if not 94 chars then error message
                    }

                    

                    //TODO shoudl be batch control, check then get that, would be an error if -1
                    if (recordType != ACHParser.BATCH_CONTROL_RECORD_TYPE_ASCII_VALUE)
                    {
                        //check if peek to next record type is batch control as expected, or something else or end of file which are errors
                        if (recordType == -1)
                        {
                            //TODO add to msg unected end of file
                        }
                        else
                        {
                            //TODO
                            devnull = reader.ReadLine();
                            ++linesRead;
                        }
                    }
                    else
                    {
                        string batchControl = reader.ReadLine();
                        //TODO if not 94 chars then error
                        if(batchControl != null)
                        {
                            string batchMessages;
                            ParseControl(batchControl, batch);

                            //TODO if batchcontrolmsg not null/ws then add to msgs
                            batchMessages = batch.Verify();

                            ++linesRead;
                        }
                        else
                        {
                            //TODO add to message that batch control was null
                        }
                    }
                }
            }
            else
            {
                //message that we got an unexpected emtpy file
            }
            return batch;
        }
    }
}
