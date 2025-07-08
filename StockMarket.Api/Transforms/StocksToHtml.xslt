<!-- StockMarket.Api/Transforms/StocksToHtml.xslt -->
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:stock="http://www.example.com/stockdata/v1"
                exclude-result-prefixes="stock">

    <xsl:output method="html" indent="yes" encoding="UTF-8"/>

    <!-- Dışarıdan alınan parametre (C# tarafından verilecek) -->
    <xsl:param name="reportGenerationTime"/>

    <xsl:template match="/">
        <html>
            <head>
                <title>Stock Market Report</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 20px; }
                    table { border-collapse: collapse; width: 100%; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    tr:nth-child(even) { background-color: #f9f9f9; }
                </style>
            </head>
            <body>
                <h1>Stock Market Report</h1>
                <table>
                    <thead>
                        <tr>
                            <th>Ticker Symbol</th>
                            <th>Company Name</th>
                            <th>Current Price</th>
                            <th>Change</th>
                            <th>% Change</th>
                            <th>Volume</th>
                            <th>Last Updated</th>
                        </tr>
                    </thead>
                    <tbody>
                        <xsl:apply-templates select="stock:StockDataFeed/stock:Stock"/>
                    </tbody>
                </table>
                <p>Generated on: <xsl:value-of select="$reportGenerationTime"/></p>
            </body>
        </html>
    </xsl:template>

    <xsl:template match="stock:Stock">
        <tr>
            <td><xsl:value-of select="@TickerSymbol"/></td>
            <td><xsl:value-of select="stock:CompanyName"/></td>
            <td><xsl:value-of select="format-number(stock:CurrentPrice, '#,##0.00')"/></td>
            <td><xsl:value-of select="format-number(stock:Change, '#,##0.00;-#,##0.00')"/></td>
            <td><xsl:value-of select="format-number(stock:PercentChange, '0.00%')"/></td>
            <td><xsl:value-of select="format-number(stock:Volume, '#,##0')"/></td>
            <td><xsl:value-of select="stock:LastUpdated"/></td>
        </tr>
    </xsl:template>

</xsl:stylesheet>
