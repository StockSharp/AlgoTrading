# Estratégia de Grade de Cobertura Frank Ud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Grade de Cobertura Frank Ud** é uma portagem direta do consultor especialista MetaTrader "Frank Ud" para a API de alto nível do StockSharp. O bot mantém simultaneamente cestas compradas e vendidas no mesmo instrumento e realiza médias no estilo martingale sempre que o preço vai contra a cesta ativa. Todo o tratamento de sinais é realizado com base em atualizações de melhor bid/ask (Level 1), tornando a estratégia adequada para execução de baixa latência ou backtesting tick a tick.

## Lógica de negociação
1. **Cobertura inicial** – quando não há posições abertas, a estratégia abre imediatamente uma ordem de compra e uma de venda a mercado com o mesmo volume. Cada ordem recebe um stop-loss e take-profit expressos em pips.
2. **Gestão de stop/take** – enquanto ambas as cestas existirem, seus níveis de proteção são respeitados. Quando o preço atinge um nível de proteção, a cesta correspondente é fechada.
3. **Gestão unilateral** – quando apenas posições compradas ou vendidas restam, a estratégia:
   - Calcula o preço médio de entrada ponderado pelo volume da cesta ativa.
   - Reatribui o take-profit comum ao preço médio ± distância configurada.
   - Remove o stop-loss (o EA original depende puramente do take-profit a partir deste ponto).
4. **Passo de martingale** – se o preço se mover contra a cesta ativa em mais do que o passo configurado, a estratégia dobra o multiplicador e abre uma nova ordem a mercado. O método auxiliar `AdjustVolume` mantém cada ordem alinhada com o passo de volume, o mínimo e o máximo do instrumento.
5. **Reinício do ciclo** – uma vez que todas as cestas estejam fechadas, o multiplicador é redefinido para 1 e um novo ciclo de cobertura começa.

## Parâmetros
- `TakeProfitPips` – distância entre o preço médio da cesta e o alvo coletivo de take-profit (padrão: 12 pips).
- `StopLossPips` – distância do stop de proteção usada apenas para as primeiras ordens de cobertura (padrão: 12 pips).
- `StepPips` – movimento adverso necessário antes de adicionar a próxima ordem de martingale (padrão: 16 pips).
- `AutoLot` – quando `true`, a estratégia usa `LotSize`; caso contrário, opera com o volume mínimo do instrumento.
- `LotSize` – tamanho de lote base personalizado usado junto com o multiplicador de martingale quando `AutoLot` está ativado.

## Notas de implementação
- A conversão usa a API de alto nível `Strategy`: as assinaturas Level 1 conduzem a lógica, e o posicionamento de ordens depende dos auxiliares `BuyMarket`/`SellMarket`.
- O rastreamento de posição é interno: a estratégia armazena o preço de entrada e o volume de cada ordem de cesta para poder reproduzir as regras de médias do MetaTrader originais.
- O multiplicador (`_multiplier`) espelha a variável `Coefficient` do EA e dobra após cada ordem adicional. Uma vez que todos os trades estejam fechados, o multiplicador é redefinido para `1`.
- `AdjustVolume` emula a função MQL5 `LotCheck` limitando volumes solicitados ao passo de negociação e aos limites de contrato permitidos.
- A estratégia requer uma conta com habilitação de cobertura, pois mantém cestas compradas e vendidas simultaneamente, assim como o EA fonte.

## Arquivos
- `CS/FrankUdStrategy.cs` – implementação principal da estratégia com comentários em inglês explicando cada bloco.
- `README.md` – este documento.
- `README_ru.md` – tradução para o russo.
- `README_zh.md` – tradução para o chinês simplificado.
