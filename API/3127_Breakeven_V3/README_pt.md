# Gerenciador Breakeven V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
O Gerenciador Breakeven V3 é uma conversão do consultor especializado MetaTrader 5 `Breakeven v3 (edição de barabashkakvn)`.
O script original não abre trades. Em vez disso, calcula continuamente o nível de break-even do portfólio para o
símbolo selecionado e move as ordens de proteção (stop-loss ou take-profit) para cada posição comprada e vendida aberta,
de modo que todo o livro seja fechado em torno desse preço de break-even com um buffer opcional.

## Lógica da estratégia
* **Reconstrução do break-even** – cada vez que um trade é executado ou chegam novas cotações, a estratégia reconstrói o
  preço médio ponderado de abertura para exposição comprada e vendida separadamente. Inclui as comissões por posição que o StockSharp
  relata nos objetos `MyTrade` para refletir a implementação MQL.
* **Cálculo do preço alvo** – o preço de break-even é deslocado por `Delta (points)` pontos MetaTrader. O deslocamento é
  adicionado quando a exposição líquida é comprada e subtraído quando é vendida, replicando o parâmetro "Delta" original.
* **Colocação de ordens de proteção** –
  * Quando a exposição líquida é comprada, um **sell limit** de take-profit é colocado para o volume comprado total e um **buy stop**
    de stop-loss é anexado ao volume vendido agregado no mesmo preço.
  * Quando a exposição líquida é vendida, um **buy limit** de take-profit é colocado para o volume vendido completo e um **sell stop**
    de stop-loss protege quaisquer coberturas compradas.
  * Se ambos os lados estiverem zerados, todas as ordens de proteção são canceladas.
* **Monitoramento de cotações e diagnósticos** – a estratégia assina atualizações de Nível 1. O bid/ask mais recente é usado para
  calcular estatísticas de distância ao alvo e um lucro flutuante estimado. Quando `Enable Logging` é true, esses valores
  são escritos no log da estratégia para emular os comentários no gráfico da versão MQL.

## Parâmetros
* **Delta (points)** – offset aplicado ao preço de break-even calculado. O valor é expresso em pontos MetaTrader,
  ou seja, um décimo de pip em símbolos FX de cinco dígitos. Padrão: `100`.
* **Enable Logging** – alterna a saída de log detalhada descrevendo o nível de break-even atual, a distância ao alvo e
  o PnL flutuante. Padrão: `true`.

## Notas de uso
* A estratégia é um gerenciador de trades. Deve ser lançada sobre uma estratégia existente ou posição manual. Ela não
  abrirá ordens de mercado por si mesma.
* Na inicialização, o código inspeciona o portfólio e reconstrói um único lote sintético para cada lado da posição usando
  o preço médio informado pelo StockSharp. Para maior precisão, manter a estratégia em execução sempre que novos trades forem abertos.
* As cobranças de swap não estão disponíveis no StockSharp, portanto apenas as informações de comissão são incluídas ao reconstruir
  o preço de break-even. Se o corretor aplica swaps noturnos, eles devem ser tratados manualmente.
* O script assume que a conta permite hedge (posições compradas e vendidas simultâneas). Se o corretor liquida posições,
  os agregados comprado e vendido se reduzirão a uma única exposição líquida assim como no MetaTrader.
* Não há versão Python deste port. Apenas a implementação C# é fornecida.
