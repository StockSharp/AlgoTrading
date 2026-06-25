# Estratégia de Oscilador Exp 2XMA Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz a lógica do assessor especialista original do MetaTrader "Exp_2XMA_Ichimoku_Oscillator" combinando dois envelopes de preço estilo Ichimoku suavizados com médias móveis configuráveis. A implementação no StockSharp usa a API de estratégia de alto nível e foca na geração de sinais baseada em velas, mantendo as regras de gerenciamento de posição do algoritmo fonte.

## Ideia principal

1. Dois pontos médios tipo Donchian são calculados no timeframe selecionado:
   - O **ponto médio rápido** calcula a média do máximo mais alto e mínimo mais baixo em `UpPeriod1` e `DownPeriod1` barras.
   - O **ponto médio lento** realiza a mesma operação com `UpPeriod2` e `DownPeriod2` barras.
2. Cada ponto médio é suavizado por uma média móvel (`Method1`, `Method2`) de comprimentos `XLength1` e `XLength2`. Os métodos de suavização disponíveis são Simples, Exponencial, Suavizado e Linear Ponderado.
3. O valor do oscilador é a diferença entre os dois pontos médios suavizados. Quatro estados de cor descrevem seu comportamento:
   - `PositiveRising` (0): oscilador acima de zero e subindo.
   - `PositiveFalling` (1): oscilador acima de zero e perdendo momentum.
   - `NegativeRising` (3): oscilador abaixo de zero mas subindo em direção ao zero.
   - `NegativeFalling` (4): oscilador abaixo de zero e caindo mais.
   - `Neutral` (2) é atribuído durante o aquecimento.
4. Os sinais são avaliados usando as cores da barra em `SignalBar` e a barra imediatamente anterior (`SignalBar + 1`), o que espelha o deslocamento de buffer na versão MQL.

## Lógica de trading

- **Entrada comprada**: permitida quando `EnableBuyOpen` é verdadeiro. Se a cor da barra mais antiga (`SignalBar + 1`) era ascendente (0 ou 3) e a barra mais recente (`SignalBar`) mudou para uma cor descendente (1 ou 4), a estratégia fecha qualquer posição vendida (`EnableSellClose`) e abre/estende uma posição comprada usando `Volume + |Position|` unidades.
- **Entrada vendida**: permitida quando `EnableSellOpen` é verdadeiro. Se a cor da barra mais antiga era descendente (1 ou 4) e a barra mais recente mudou para uma cor ascendente (0 ou 3), a estratégia fecha os comprados existentes (`EnableBuyClose`) e abre/estende uma posição vendida com `Volume + |Position|` unidades.
- Todas as execuções ocorrem no fechamento da vela que gera o gatilho. As ordens são sempre a mercado e a estratégia não aplica níveis adicionais de stop-loss ou take-profit; depende exclusivamente das transições de cor para as saídas.
- `StartProtection()` é ativado na inicialização para usar as verificações de segurança integradas do framework para posições inesperadas.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Período usado para cálculos do indicador. | Velas de 4 horas |
| `UpPeriod1`, `DownPeriod1` | Janelas de retrospecto para o ponto médio rápido. | 6, 6 |
| `UpPeriod2`, `DownPeriod2` | Janelas de retrospecto para o ponto médio lento. | 9, 9 |
| `XLength1`, `XLength2` | Comprimentos de suavização para as duas médias móveis. | 25, 80 |
| `Method1`, `Method2` | Tipos de média móvel (Simples, Exponencial, Suavizado, Ponderado). | Simples |
| `SignalBar` | Deslocamento de barra histórica usado para ler cores do oscilador. | 1 |
| `EnableBuyOpen`, `EnableSellOpen` | Alternar entradas compradas/vendidas. | true |
| `EnableBuyClose`, `EnableSellClose` | Alternar saídas compradas/vendidas. | true |
| `Volume` | Tamanho base da operação; posições existentes são somadas a este valor ao reverter. | 1 |

## Notas de uso

- Os tipos de média móvel cobrem os comportamentos de suavização mais comuns do especialista original. Opções avançadas como ajustes de fase XMA personalizados não estão disponíveis no StockSharp e foram substituídas por indicadores padrão.
- Como o oscilador é calculado em velas fechadas, os sinais aparecem com o mesmo atraso de uma barra que a implementação MQL usava (`SignalBar = 1`). Aumente `SignalBar` se precisar de barras de confirmação adicionais.
- Considere combinar a estratégia com gerenciamento de risco externo (gestor de portfólio, stops protetores) ao operar em mercados ao vivo, pois as saídas dependem exclusivamente das reversões de cor do oscilador.
