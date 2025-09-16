<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
      xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output method="html" indent="yes" />

	<xsl:template match="/">
		<html>
			<head>
				<style>
					body { font-family: Arial; font-size: 12px; }
					h2 { text-align: center; }
					table { width: 100%; border-collapse: collapse; margin-top: 10px; }
					th, td { border: 1px solid #333; padding: 6px; text-align: left; }
					th { background-color: #f2f2f2; }
				</style>
			</head>
			<body>
				<h2>Delivery Challan</h2>
				<p>
					<b>Challan No:</b>
					<xsl:value-of select="DeliveryChallan/ChallanNo" />
				</p>
				<p>
					<b>Receiver:</b>
					<xsl:value-of select="DeliveryChallan/ReceiverName" />
				</p>
				<p>
					<b>Mobile:</b>
					<xsl:value-of select="DeliveryChallan/ReceiverMobile" />
				</p>
				<p>
					<b>Date:</b>
					<xsl:value-of select="DeliveryChallan/Date" />
				</p>

				<table>
					<tr>
						<th>Particular</th>
						<th>Quantity</th>
						<th>Remarks</th>
					</tr>
					<xsl:for-each select="DeliveryChallan/Items">
						<tr>
							<td>
								<xsl:value-of select="Particular" />
							</td>
							<td>
								<xsl:value-of select="Quantity" />
							</td>
							<td>
								<xsl:value-of select="Remarks" />
							</td>
						</tr>
					</xsl:for-each>
				</table>
			</body>
		</html>
	</xsl:template>
</xsl:stylesheet>
