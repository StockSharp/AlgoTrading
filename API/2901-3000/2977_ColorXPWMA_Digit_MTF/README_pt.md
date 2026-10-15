# Estratégia ColorXPWMA Digit Multi-Período
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia converte o consultor especialista MetaTrader 5 **Exp_ColorXPWMA_Digit_NN3_MMRec** para a API de alto nível do StockSharp. O robô original opera três módulos independentes que negociam em diferentes períodos analisando a coloração digital da média móvel ColorXPWMA. O port do StockSharp mantém o mesmo comportamento: cada módulo observa sua própria série de velas, fecha posições quando o indicador muda de cor e opcionalmente abre uma nova operação na direção detectada.

A configuração padrão segue o modelo MT5:

| Módulo | Período | Stop Loss (pontos) | Take Profit (pontos) |
| ------ | ------- | ------------------ | -------------------- |
| A | 8 horas | 3000 | 10000 |
| B | 4 horas | 2000 | 6000 |
| C | 1 hora | 1000 | 3000 |

Cada módulo pode ser habilitado ou desabilitado para entradas e saídas compradas e vendidas através de parâmetros booleanos dedicados. A implementação mantém rastreamento de posições individuais por módulo para que operações compradas e vendidas simultâneas possam coexistir sem interferir na contabilidade de volume dos outros períodos.

## Indicador ColorXPWMA Digit
O indicador ColorXPWMA Digit emula o indicador personalizado MT5. Para cada vela terminada o algoritmo:

1. Constrói uma média ponderada por potência do preço aplicado selecionado (`Period` e `Power`).
2. Suaviza o valor com a média móvel escolhida (`SmoothMethods` e `SmoothLength`).
3. Arredonda o resultado para o número configurado de casas decimais (`Digit`).
4. Atribui um código de cor: **2** quando o valor suavizado aumenta, **0** quando diminui, caso contrário a cor anterior é reutilizada.

`SignalBar` controla qual barra histórica é inspecionada. O valor `0` usa a vela fechada mais recente, o valor `1` a vela anterior, etc. Uma oportunidade de compra aparece quando a barra monitorada muda para a cor `2` depois de ser diferente na barra anterior. Uma oportunidade de venda é gerada quando a cor se torna `0` depois de ser diferente na barra anterior.

Os métodos de suavização são mapeados para os indicadores do StockSharp da seguinte forma:

- `Sma`, `Ema`, `Smma`, `Lwma`, `Jjma` → médias móveis correspondentes do StockSharp.
- `T3` → implementação interna do Tillson T3.
- `Vidya` → implementação interna do VIDYA impulsionada pelo Oscilador de Momentum de Chande.
- `Ama` → Média Móvel Adaptativa de Kaufman.
- Opções não suportadas (`JurX`, `Parabolic`) recaem na média móvel simples, correspondendo ao comportamento do modelo original quando suavizadores exóticos não estão disponíveis.

## Gestão de operações e gestão monetária
Para cada módulo a estratégia mantém duas posições virtuais independentes (comprada e vendida). Quando um módulo recebe um sinal de fechamento, a estratégia envia uma ordem a mercado igual ao volume restante dessa posição virtual. As ordens de abertura são ignoradas enquanto uma posição oposta ainda estiver aberta.

O dimensionamento da posição copia o auxiliar de gestão monetária do MT5:

- `NormalMM` define o volume base.
- `SmallMM` substitui o volume base quando operações recentes registraram pelo menos `LossTrigger` perdas dentro das últimas `TotalTrigger` operações para essa direção.

A lógica é avaliada separadamente para sequências compradas e vendidas. Os resultados das operações são calculados a partir do preço médio preenchido quando um módulo fecha completamente sua posição virtual.

O gerenciamento de riscos espelha os stops do MT5 em pontos de preço:

- Quando uma posição comprada está aberta e as mínimas das velas cruzam `entry - StopLoss * PriceStep`, a posição comprada é fechada imediatamente.
- Quando as máximas das velas tocam `entry + TakeProfit * PriceStep`, os lucros são obtidos.
- As regras são espelhadas para posições vendidas (`entry + StopLoss` para proteção, `entry - TakeProfit` para alvos).

## Parâmetros
Todos os parâmetros são expostos através de objetos `StrategyParam<T>` e podem ser otimizados no designer do StockSharp. Eles são agrupados por módulo (A, B, C). A tabela a seguir lista as configurações para qualquer módulo **X**:

| Parâmetro | Descrição |
| --------- | ----------- |
| `X_CandleType` | Série de velas a se inscrever (períodos padrão mostrados acima). |
| `X_Period`, `X_Power` | Janela ponderada por potência usada para construir o valor base XPWMA. |
| `X_SmoothMethod`, `X_SmoothLength`, `X_SmoothPhase` | Suavizador aplicado ao preço ponderado. `SmoothPhase` é mantido por compatibilidade com usuários MT5 JJMA. |
| `X_AppliedPrice` | Fonte de preço (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow, DeMark). |
| `X_Digit` | Precisão de arredondamento aplicada ao valor suavizado. |
| `X_SignalBar` | Barra histórica usada para avaliação de sinais. |
| `X_BuyMagic`, `X_SellMagic` | Preservados para rastreabilidade (usados dentro dos comentários de ordens). |
| `X_BuyTotalTrigger`, `X_BuyLossTrigger` | Limites de gestão monetária do lado comprado. |
| `X_SellTotalTrigger`, `X_SellLossTrigger` | Limites de gestão monetária do lado vendido. |
| `X_SmallMM`, `X_NormalMM` | Volumes usados pela regra de gestão monetária. |
| `X_MarginMode`, `X_Deviation` | Campos reservados mantidos para paridade de recursos; não alteram as ordens do StockSharp. |
| `X_StopLoss`, `X_TakeProfit` | Distâncias em passos de preço aplicadas à posição virtual do módulo. |
| `X_BuyOpen`, `X_SellOpen`, `X_SellClose`, `X_BuyClose` | Chaves de permissão para ações do módulo. |

## Notas
- Cada ordem a mercado é anotada com `A|BuyOpen`, `B|SellClose`, etc. para que os preenchimentos possam ser rastreados até seu módulo.
- A estratégia opera exclusivamente em velas terminadas e portanto reproduz a proteção `IsNewBar` do MT5 fornecida automaticamente pela API de alto nível.
- Se múltiplos módulos forem acionados na mesma barra, seus volumes são processados sequencialmente usando os buffers de posição virtual por módulo.
