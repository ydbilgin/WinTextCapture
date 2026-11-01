@grid features columns=2,3
---
<table>
{{#features_rows}}
<tr>
{{#cells}}
<td width="{{features_cell_width}}%" valign="top">

{{^filler}}
**{{name}}**<br>
{{text}}
{{/filler}}

</td>
{{/cells}}
</tr>
{{/features_rows}}
</table>
