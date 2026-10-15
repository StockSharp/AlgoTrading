# Estratégia do comerciante Alliheik
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do consultor especialista MetaTrader 4 **alliheik.mq4**. A estratégia combina um corpo de vela Heiken Ashi duplamente suavizado com a média móvel de "mandíbula" Alligator deslocada para frente. As entradas ocorrem quando os buffers Heiken Ashi cruzam após o processo de suavização. As saídas contam com um filtro de crossover de mandíbula, alvos fixos opcionais e um trailing stop baseado em preço.

## Lógica de negociação

- **Construção Heiken Ashi**
  - Preços brutos de abertura, máximo, mínimo e fechamento suaves com `PreSmoothMethod` / `PreSmoothPeriod`.
  - Construa velas Heiken Ashi clássicas a partir dos preços suavizados.
  - Troque os buffers de alta/baixa dependendo da cor da vela (a alta mantém a ordem baixa/alta, a baixa as inverte).
  - Aplique uma segunda passagem de suavização (`PostSmoothMethod` / `PostSmoothPeriod`) aos buffers condicionais. Estes são os valores comparados nas regras de sinalização.
- **Definição de sinal**
  - **Longo**: o buffer inferior atual está abaixo do buffer superior enquanto a barra anterior tinha relação oposta ou igual.
  - **Curto**: o buffer inferior atual está acima do buffer superior enquanto a barra anterior tinha relação oposta ou igual.
- **Filtro de mandíbula e trilha**
  - A mandíbula Alligator é uma média móvel de `JawsPeriod` barras, deslocada `JawsShift` barras para frente e alimentada com `JawsPrice`.
  - `Close[6]` (seis compassos atrás) deve cruzar a mandíbula antes que a posição possa ser fechada automaticamente.
  - Uma vez que a diferença entre `Close[6]` e a mandíbula atinge oito pontos e inverte através da mandíbula, a posição é fechada.
  - Se `TrailingStopPoints` for maior que zero, o preço stop segue `Close[6]` quando a vela estiver no lado lucrativo da mandíbula.
- **Paradas e metas**
  - `StopLossPoints` e `TakeProfitPoints` são distâncias fixas opcionais aplicadas na entrada.
  - A lógica de trailing substitui o stop de proteção quando ele se move a favor da negociação.

## Parâmetros padrão

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Período usado para todos os cálculos. |
| `JawsPeriod` | 144 | Comprimento da média móvel da mandíbula Alligator. |
| `JawsShift` | 8 | Deslocamento da mandíbula para frente (número de barras). |
| `JawsMethod` | Simples | Tipo de média móvel para a mandíbula (Simples, Exponencial, Suavizada, Ponderada). |
| `JawsPrice` | Fechar | Componente de preço fornecido à mandíbula (Fechar/Abrir/Alta/Baixa/Mediana/Típica/Ponderada). |
| `PreSmoothMethod` | Exponencial | Média móvel usada para suavizar valores brutos OHLC antes de calcular Heiken Ashi. |
| `PreSmoothPeriod` | 21 | Período das médias de pré-suavização. |
| `PostSmoothMethod` | Ponderado | Média móvel aplicada aos buffers condicionais Heiken Ashi. |
| `PostSmoothPeriod` | 1 | Período das médias pós-suavização (1 mantém os buffers originais). |
| `StopLossPoints` | 0 | Distância de parada fixa em pontos (0 desabilita). |
| `TrailingStopPoints` | 0 | Distância de parada final com base em `Close[6]` (0 desativa). |
| `TakeProfitPoints` | 225 | Distância de lucro em pontos (0 desabilita). |
| `OrderVolume` | 0,1 | Tamanho do lote para entradas. |

## Indicadores usados

- MAs de pré-suavização (quatro séries paralelas para abertura, alta, baixa e fechamento).
- Reconstrução de Heiken Ashi impulsionada pelos preços suavizados.
- MAs pós-suavização dos buffers condicionais que formam o sinal de entrada.
- Alligator média móvel da mandíbula com tipo ajustável, deslocamento e preço aplicado.

## Resumo de entrada e saída

- **Insira Long** quando o buffer inferior suavizado cruzar abaixo do buffer superior e a barra anterior não for de alta (condição de cruzamento descrita acima).
- **Saída longa** quando:
  - `Close[6]` cai abaixo da mandíbula após estar anteriormente acima dela e a distância atingiu ≥ 8 pontos; ou
  - `TakeProfitPoints` meta foi atingida; ou
  - A parada `StopLossPoints`/`TrailingStopPoints` foi atingida.
- **Insira Short** quando o buffer inferior suavizado cruzar acima do buffer superior e a barra anterior não for de baixa.
- **Sair curto** quando:
  - `Close[6]` sobe novamente acima da mandíbula após estar anteriormente abaixo dela e a distância atingiu ≥ 8 pontos; ou
  - `TakeProfitPoints` meta foi atingida; ou
  - A parada `StopLossPoints`/`TrailingStopPoints` foi atingida.

## Notas de conversão

- A estratégia impõe uma negociação por barra, espelhando a verificação `isOrderAllowed()` no EA original.
- Paradas e metas de proteção são simuladas internamente porque as estratégias StockSharp não podem depender de ordens MT4 do lado da corretora.
- A média móvel da mandíbula armazena valores históricos para que o deslocamento para frente replique o comportamento `iMA` com `ma_shift = JawsShift`.
- Todos os cálculos usam aritmética decimal e ligações de indicadores consistentes com StockSharp requisitos de alto nível API.

## Risco e uso

- Projetado para negociações longas e curtas no mesmo instrumento.
- Funciona melhor em mercados de tendências, onde a mudança de mandíbula e a suavização de Heiken Ashi podem destacar oscilações de médio prazo.
- Considere ajustar `TrailingStopPoints` e `TakeProfitPoints` para corresponder à volatilidade do instrumento.
- Sempre faça backtest e forward test em contas em papel antes da implantação ativa.
