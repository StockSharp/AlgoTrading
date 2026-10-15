# Estratégia XROC2 VG com Filtro de Tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o assessor especializado do MetaTrader **Exp_XROC2_VG_Tm** usando a API de alto nível do StockSharp. Constrói duas curvas suavizadas de taxa de variação (ROC) e abre operações contrárias quando a curva mais rápida cruza a mais lenta. Um filtro de sessão de trading e alvos de proteção opcionais reproduzem as configurações originais de gestão de capital.

## Lógica de trading

- Dois valores ROC são calculados a partir do preço de fechamento usando períodos de retrocesso independentes.
- Cada stream ROC é suavizado com um método de média móvel configurável.
- Os sinais são avaliados em um índice de barra deslocado, correspondendo ao comportamento original de `SignalBar`.
- Quando a linha rápida estava acima da lenta na barra anterior, mas cai abaixo dela na barra de sinal, a estratégia fecha qualquer posição vendida e pode abrir uma posição comprada.
- Quando a linha rápida estava abaixo da lenta na barra anterior, mas sobe acima dela na barra de sinal, a estratégia fecha qualquer posição comprada e pode abrir uma posição vendida.
- Uma janela de trading opcional pode liquidar todas as posições fora da sessão permitida antes de colocar novas operações.

O lado da ordem muda apenas após a posição anterior estar completamente fechada, imitando os algoritmos de trading do MetaTrader.

## Indicadores

- **ROC rápido** – Momentum, porcentagem ou razão de variação de preço ao longo de `RocPeriod1` barras, suavizado com `SmoothMethod1` e comprimento `SmoothLength1`.
- **ROC lento** – Mesmo cálculo ao longo de `RocPeriod2` barras, suavizado com `SmoothMethod2` e comprimento `SmoothLength2`.
- Métodos de suavização suportados: médias móveis Simples, Exponencial, Suavizada (RMA) e Ponderada. As opções originais JJMA/VIDYA/AMA são aproximadas por suavização exponencial.

## Gestão de risco

- `StopLoss` e `TakeProfit` especificam saídas opcionais de distância fixa em unidades de preço absoluto. Quando qualquer limite é atingido, a posição é fechada imediatamente.
- `OrderVolume` define o tamanho de todas as novas posições.
- Saídas baseadas em indicadores também podem liquidar posições mesmo se os alvos de proteção estiverem desabilitados.

## Filtro de sessão

- `UseTimeFilter` ativa/desativa a janela horária do dia.
- `StartTime` / `EndTime` especificam os limites da sessão. Quando o intervalo ultrapassa a meia-noite, a janela é tratada como dois segmentos, exatamente como na versão MQL.
- Se uma posição ainda estiver aberta quando a janela fechar, ela será liquidada a mercado antes de a estratégia avaliar novas entradas.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Tipo de dados de vela usado para cálculos (padrão: velas de 4 horas). |
| `RocPeriod1`, `RocPeriod2` | Comprimentos de retrocesso para os streams ROC rápido e lento. |
| `SmoothLength1`, `SmoothLength2` | Comprimentos de suavização para cada stream. |
| `SmoothMethod1`, `SmoothMethod2` | Tipos de média móvel aplicados às saídas ROC. |
| `RocType` | Fórmula de cálculo ROC: momentum, variação percentual ou razão. |
| `SignalShift` | Número de barras concluídas para trás usadas para ler os valores de sinal. |
| `AllowBuyOpen`, `AllowSellOpen` | Habilitar ou desabilitar abertura de posições compradas/vendidas. |
| `AllowBuyClose`, `AllowSellClose` | Habilitar ou desabilitar saídas baseadas em indicadores para posições compradas/vendidas. |
| `UseTimeFilter` | Ativa a janela de sessão de trading. |
| `StartTime`, `EndTime` | Horários de início e fim da sessão. |
| `OrderVolume` | Volume para cada nova operação. |
| `StopLoss`, `TakeProfit` | Distâncias absolutas opcionais para saídas de proteção. |

## Notas de implementação

- A estratégia mantém históricos curtos de preços e valores suavizados em vez de usar buffers de indicadores, o que reproduz o offset `SignalBar` original sem depender de `GetValue`.
- Os suavizamentos JJMA, VIDYA e AMA do indicador MQL são mapeados para suavização exponencial para permanecer dentro do conjunto de indicadores padrão do StockSharp.
- Todos os comentários no código estão em inglês e o namespace segue as diretrizes do repositório.
