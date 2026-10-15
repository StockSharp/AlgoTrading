# Estratégia Twenty 200 Pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o especialista original **20/200 pips** do MQL5. Examina velas horárias e compara dois preços de abertura históricos (`Open[t1]` e `Open[t2]`). Quando a diferença entre essas aberturas excede um delta configurável durante uma hora específica, a estratégia entra em uma única negociação para a sessão e depende de níveis fixos de take-profit e stop-loss.

## Lógica de negociação
1. Assinar velas horárias (configurável) e alimentar o preço de abertura em dois indicadores `Shift` para recuperar as aberturas nos índices necessários.
2. Durante cada vela concluída, redefinir a flag "pode negociar" assim que a hora atual for maior que a hora de negociação configurada. Isso espelha o reset diário no consultor especialista original.
3. Quando a hora coincide com a hora de negociação configurada e nenhuma posição está aberta, comparar os preços de abertura armazenados:
   - Se `Open[t1] > Open[t2] + delta`, enviar uma ordem de **venda** de mercado.
   - Se `Open[t1] + delta < Open[t2]`, enviar uma ordem de **compra** de mercado.
4. Após enviar uma ordem, a estratégia proíbe novas entradas até o próximo reset diário. As ordens de take-profit e stop-loss de proteção são gerenciadas via `StartProtection`.

## Parâmetros
- `TakeProfit` – distância em pontos de preço para a ordem de take-profit (padrão 200 pontos).
- `StopLoss` – distância em pontos de preço para a ordem de stop-loss (padrão 2000 pontos).
- `TradeHour` – hora do dia em que a verificação de entrada é realizada (padrão 18).
- `FirstOffset` – índice do preço de abertura mais antigo (corresponde a `Open[t1]` no script MQL, padrão 7).
- `SecondOffset` – índice do preço de abertura mais recente (`Open[t2]`, padrão 2).
- `DeltaPoints` – diferença mínima em pontos entre as duas aberturas para acionar uma negociação (padrão 70).
- `Volume` – tamanho da ordem usado para entradas de mercado (padrão 0.1).
- `CandleType` – período de tempo usado para cálculos (padrão velas de 1 hora).

## Notas de implementação
- Os indicadores `Shift` são processados manualmente para acessar preços de abertura históricos sem manter coleções personalizadas.
- A estratégia chama `StartProtection` uma vez durante `OnStarted` para emular os níveis de stop-loss/take-profit definidos no especialista MQL.
- Comentários em inglês são incluídos diretamente no código para facilitar a manutenção e revisão.
- Apenas uma negociação por dia é permitida porque `_canTrade` é limpo logo após uma ordem ser colocada e só é restaurado depois que a hora de negociação configurada passar.

## Uso
1. Anexar a estratégia a um instrumento e configurar os parâmetros de acordo com o instrumento alvo.
2. Certificar-se de que o instrumento tem um `PriceStep` válido; ele é usado para converter parâmetros baseados em pontos em distâncias de preço absolutas.
3. Iniciar a estratégia. Ela esperará até a hora configurada e agirá na próxima vela concluída se as condições de preço de abertura forem atendidas.
