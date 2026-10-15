# A Estratégia da Feiticeira
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Enchantress replica o comportamento de autoaprendizagem do consultor especialista MQL4 com o mesmo nome. O original EA
classifica cada vela acabada em dez baldes, mantém um histórico contínuo dos últimos sete baldes e lança a compra “virtual”
e pedidos de venda para cada novo padrão de sete velas. Sempre que o preço atingir posteriormente os níveis virtuais de take-profit ou stop-loss, o
padrão recebe uma pontuação positiva ou negativa. As negociações ao vivo são acionadas apenas quando o padrão atual de sete velas pertence ao
padrões virtuais de alto desempenho. Esta porta StockSharp preserva esse ciclo de feedback e expõe todas as opções críticas de configuração
como parâmetros de estratégia.

## Classificação de velas

1. Cada vela finalizada é avaliada uma vez, usando seus preços de abertura, fechamento, máximo e mínimo.
2. A direção do corpo divide as velas em baixa (dígitos `0–4`) e alta (dígitos `5–9`).
3. A proporção alto/baixo `100 - Low * 100 / High` determina o dígito exato dentro de cada grupo:
   - `0/5` para intervalos muito pequenos (≤ 0,04)
   - `1/6` para intervalos pequenos (0,04 – 0,15)
   - `2/7` para faixas médias (0,15 – 0,25)
   - `3/8` para intervalos amplos (0,25 – 0,40)
   - `4/9` para intervalos extremamente amplos (> 0,40)
4. O último dígito é anexado à janela contínua de sete caracteres que representa o padrão atual do mercado.

Esta classificação corresponde aos intervalos numéricos produzidos pela rotina `ManagePatterns` do EA original.

## Mecanismo de pedido virtual

- Assim que sete dígitos estiverem disponíveis, a estratégia cria um conjunto emparelhado de ordens virtuais (longas e curtas) para o padrão ativo.
- O preço de entrada virtual é igual ao fechamento da vela. Paradas e alvos virtuais são derivados de `VirtualStopLoss` e
`VirtualTakeProfit` usando a etapa de preço do instrumento.
- Nas velas subsequentes, a estratégia verifica se a máxima/baixa da vela toca os alvos virtuais ou para:
  - Um alvo atingido contribui `+1` para a respectiva pontuação de alta ou baixa.
  - Um stop hit contribui com `-3` para a respectiva pontuação, reproduzindo a penalidade aplicada pelo EA.
- Ordens virtuais fechadas são descartadas para manter o uso de memória limitado, enquanto as pontuações acumuladas permanecem anexadas aos seus
chave padrão de sete dígitos.

## Geração de sinal

Antes de processar a próxima vela, a estratégia inspeciona o padrão atual de sete dígitos (construído apenas a partir de velas anteriores). Negociar é
permitido de segunda a quinta; As sextas-feiras são ignoradas exatamente como na versão MQL. As seguintes regras se aplicam:

1. Construa os dez melhores padrões de alta e baixa por pontuação (apenas pontuações ≥ 1 são consideradas).
2. Se o padrão atual pertencer ao conjunto de líderes de alta, faça uma compra no mercado. Se pertencer ao conjunto de líderes de baixa, coloque um
vender no mercado. A mesma vela não pode acionar duas entradas porque a estratégia registra o carimbo de data/hora da vela após o primeiro preenchimento.
3. Após cada decisão, a vela recém-concluída é anexada à janela do padrão e aos pedidos virtuais para o novo padrão.
são lançados.

## Ordens de proteção e dimensionamento

- As negociações reais usam distâncias `StopLoss` e `TakeProfit` expressas em pips. Ambos os parâmetros são traduzidos em diferenças de preços através
a etapa do preço do título e aplicada por meio de `SetStopLoss`/`SetTakeProfit` logo após o preenchimento da ordem de mercado.
- O dimensionamento de posição pode operar em dois modos:
  - **Lote fixo**: `LotSize` é usado literalmente (ajustado às restrições de passo/min/máx do volume de troca).
  - **Gerenciamento de dinheiro de risco**: o volume é igual a `PortfolioValue / 100000 * RiskPercent`. Isso reflete o original `AccountFreeMargin`
fórmula e volta para o lote fixo se nenhum valor do portfólio estiver disponível.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `LotSize` | Tamanho fixo do pedido quando o gerenciamento de dinheiro está desativado. | `0.01` |
| `UseRiskMoneyManagement` | Alterne o bloco de dimensionamento dinâmico. | `true` |
| `RiskPercent` | Percentual do valor da carteira utilizado na modalidade de risco. | `15` |
| `StopLoss` | Distância real de stop-loss em pips. | `60` |
| `VirtualStopLoss` | Distância de parada usada para pontuação virtual. | `55` |
| `TakeProfit` | Distância real de lucro em pips. | `19` |
| `VirtualTakeProfit` | Distância de lucro para pontuação virtual. | `25` |
| `CandleType` | Prazo das velas processadas. | `5m` |

## Notas de uso

- Certifique-se de que os metadados de segurança (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) estejam preenchidos; caso contrário, dimensionamento e pip
as conversões voltam aos padrões genéricos.
- A avaliação de portfólio (`Portfolio.CurrentValue` ou `Portfolio.BeginValue`) deve estar disponível para que o dimensionamento baseado em risco funcione.
- A estratégia opera apenas em velas finalizadas e não realiza verificações de ordens virtuais intra-barra. A comparação alto/baixo é a
aproximação mais próxima da lógica baseada em ticks do MT4.
- Para aquecer o banco de dados de padrões mais rapidamente, execute a estratégia em dados históricos no modo backtesting – a lógica de pontuação é idêntica em
simulação e negociação ao vivo.
