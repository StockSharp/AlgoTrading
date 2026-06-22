# Estratégia do Indicador de Potência Total X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia recria o comportamento do expert MetaTrader "Exp_TotalPowerIndicatorX" usando as APIs de alto nível do StockSharp. Ela depende de uma implementação personalizada do Indicador de Potência Total que mede o domínio de touros e ursos contando quantos candles em uma janela deslizante fecham acima ou abaixo de uma linha base EMA interna. As decisões de negociação são tomadas quando as linhas de força altista e baixista se cruzam.

O indicador funciona com qualquer símbolo e período. Por padrão a estratégia assina candles de 4 horas, correspondendo à configuração original do expert advisor, mas o período pode ser ajustado através de um parâmetro.

## Lógica de negociação
1. Para cada candle finalizado, a estratégia alimenta o Indicador de Potência Total com os dados do candle. O indicador:
   - Calcula uma EMA com período **Power Period**.
   - Conta quantos candles dentro de **Lookback Period** tiveram `High > EMA` (touros) e `Low < EMA` (ursos).
   - Converte as contagens em valores de força no estilo percentual no intervalo 0–100.
2. Um **cruzamento altista** (força altista subindo acima da baixista) aciona uma entrada comprada quando o trading comprado está habilitado e não há posições abertas.
3. Um **cruzamento baixista** (força baixista subindo acima da altista) aciona uma entrada vendida quando o trading vendido está habilitado e não há posições abertas.
4. Cruzamentos opostos fecham posições existentes quando as chaves de saída relevantes estão habilitadas.
5. Um filtro de sessão de negociação opcional força o fechamento de todas as posições fora da janela de tempo configurada e desabilita novas entradas durante esse período.
6. Os níveis opcionais de stop-loss e take-profit são expressos em múltiplos do passo de preço do ativo. Eles são recalculados após cada entrada e saem assim que o máximo ou mínimo do candle ultrapassa o nível.

## Parâmetros
- **Candle Type** – período usado para cálculos do indicador. Padrão: candles de 4 horas.
- **Power Period** – comprimento da EMA dentro do indicador; espelha o input MQL. Padrão: 10.
- **Lookback** – número de candles usados para contar o domínio altista e baixista. Padrão: 45.
- **Volume** – tamanho da ordem enviada à bolsa ou simulador. Padrão: 1.
- **Enable Long Entry / Enable Short Entry** – permitir ou proibir novas posições na direção correspondente.
- **Enable Long Exit / Enable Short Exit** – fechar posições em sinais opostos. Desabilitar para manter posições abertas até fechamento manual ou stop-out.
- **Use Trading Hours** – habilitar o filtro de tempo. Quando ativo, a estratégia negocia apenas entre **Start Hour/Minute** e **End Hour/Minute** e fecha qualquer posição aberta fora desse intervalo. Janelas noturnas (início posterior ao fim) são suportadas.
- **Stop Loss Points / Take Profit Points** – distâncias do preço de entrada medidas em passos de preço. Defina como zero para desabilitar o nível. O cálculo usa `Security.PriceStep`, portanto garanta que os metadados do ativo estejam disponíveis.

## Notas
- A estratégia abre uma nova posição somente quando não há nenhuma posição ativa no ativo, emulando o comportamento do expert original.
- Como os cálculos de stop-loss e take-profit dependem do passo de preço do instrumento, executar a estratégia sem esses metadados mantém os níveis de proteção desabilitados automaticamente.
- O valor do indicador é plotado na área do gráfico quando a interface do usuário está disponível, o que ajuda a visualizar os cruzamentos entre a força altista e baixista.
