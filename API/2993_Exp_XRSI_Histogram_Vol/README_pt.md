# Estratégia Exp XRSI Histograma Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Esta estratégia é uma conversão em C# do consultor especialista MQL5 original `Exp_XRSI_Histogram_Vol`. Ela negocia rompimentos no histograma RSI ponderado por volume interpretando os cinco estados de cor produzidos pelo indicador. O script roda em qualquer período fornecido através da assinatura de velas e é construído sobre a API de estratégia de alto nível do StockSharp.

## Lógica da estratégia

1. Calcular um RSI no período selecionado e subtrair 50 para centralizar o oscilador.
2. Multiplicar o valor RSI centrado pelo fluxo de volume escolhido (ticks ou volume real) para enfatizar velas com atividade intensa.
3. Suavizar tanto o RSI ponderado quanto o volume bruto usando o mesmo método de média móvel e comprimento.
4. Construir limites adaptativos multiplicando o volume suavizado por quatro multiplicadores definidos pelo usuário. O histograma resultante é classificado nos seguintes estados de cor:
   - **0** – impulso otimista forte (acima de `HighLevel2`).
   - **1** – impulso otimista moderado (entre `HighLevel1` e `HighLevel2`).
   - **2** – zona neutra.
   - **3** – impulso pessimista moderado (entre `LowLevel2` e `LowLevel1`).
   - **4** – impulso pessimista forte (abaixo de `LowLevel2`).
5. As regras de entrada e saída espelham a lógica MQL:
   - Entrar no primeiro comprado quando o histograma muda para o estado **1** após estar acima do estado **1** (a cor diminui de pessimista/neutro para moderadamente otimista).
   - Entrar no segundo comprado quando o histograma muda para o estado **0** após estar acima do estado **0**.
   - Entrar no primeiro vendido quando o histograma muda para o estado **3** após estar abaixo do estado **3**.
   - Entrar no segundo vendido quando o histograma muda para o estado **4** após estar abaixo do estado **4**.
   - Fechar posições vendidas quando o histograma está nos estados **0** ou **1**.
   - Fechar posições compradas quando o histograma está nos estados **3** ou **4**.
6. A geração de sinais pode ser deslocada para trás por `SignalBar` barras para imitar a indexação do buffer do indicador original.

Duas entradas de escalonamento são suportadas para cada direção através dos multiplicadores `Mm1` e `Mm2`. Os métodos auxiliares achatam uma posição oposta antes de abrir uma nova, replicando o comportamento do código de gerenciamento de negociação legado.

## Gerenciamento de dinheiro e proteção

- `Mm1` e `Mm2` são multiplicadores de volume aplicados à propriedade `Volume` da estratégia (um padrão de 1 é usado quando `Volume` não está definido).
- Stop-loss e take-profit globais são ativados através de `StartProtection` quando tanto o passo de preço quanto os valores de pontos correspondentes são positivos. Eles são interpretados como um número de passos de preço.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Período usado para velas e cálculos de indicadores. |
| `RsiPeriod` | Comprimento do RSI. |
| `VolumeMode` | Escolha entre volume de ticks e volume real. O modo de ticks retorna para uma unidade quando os dados de volume estão ausentes. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores que escalam o volume suavizado para construir limites do histograma. |
| `MaMethod`, `MaLength`, `MaPhase` | Configurações de suavização. Métodos não suportados (Parabolic, T3, Vidya, Ama) retornam para média móvel simples. `MaPhase` é mantido por completude, mas só afeta métodos avançados como Jurik. |
| `SignalBar` | Quantas barras fechadas para trás devem ser avaliadas ao ler a cor do histograma. |
| `Mm1`, `Mm2` | Multiplicadores de volume para a primeira e segunda posição em cada direção. |
| `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` | Habilitar ou desabilitar a lógica de abertura e fechamento para comprados/vendidos. |
| `StopLossPoints`, `TakeProfitPoints` | Deslocamentos de proteção expressos em passos de preço. |

## Valores padrão

- Tipo de vela: período de 4 horas.
- Comprimento RSI: 14.
- Modo de volume: volume de ticks.
- Limites do histograma: `HighLevel2 = 17`, `HighLevel1 = 5`, `LowLevel1 = -5`, `LowLevel2 = -17`.
- Média móvel: SMA com comprimento 12 e fase 15.
- Deslocamento da barra de sinal: 1 barra.
- Gerenciamento de dinheiro: `Mm1 = 0.1`, `Mm2 = 0.2`.
- Stops: stop loss 1000 pontos, take profit 2000 pontos (aplicados apenas quando um passo de preço válido está disponível).

## Notas

- A estratégia depende de velas terminadas e ignora atualizações não terminadas.
- O suavização Jurik é suportada via `JurikMovingAverage` do StockSharp. Outros métodos legados (ParMA, T3, VIDYA, AMA) revertem para SMA devido à falta de equivalentes nativos.
- O indicador usa o `TotalVolume` da vela. Quando o volume é zero, o modo de ticks usa um peso de fallback de um para evitar suprimir sinais.
- Para análise visual, o RSI em si é plotado junto com velas e marcadores de negociações. Você pode anexar elementos de gráfico adicionais se diagnósticos mais profundos forem necessários.
