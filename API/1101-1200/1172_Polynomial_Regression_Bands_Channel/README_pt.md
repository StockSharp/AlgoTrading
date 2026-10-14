# Estratégia de Canal de Bandas de Regressão Polinomial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia ajusta uma linha de regressão polinomial aos preços recentes e constrói bandas superior e inferior a partir do desvio padrão dos resíduos. Posições compradas são abertas quando o preço cai abaixo da banda inferior e posições vendidas quando o preço sobe acima da banda superior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close < LowerBand`.
  - **Vendido**: `Close > UpperBand`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 100.
  - `Degree` = 2.
  - `StdDevMultiplier` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Regressão polinomial
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Cálculo e execução

Cada vela concluída ajusta por mínimos quadrados um polinômio de grau `Degree` aos últimos `Length` fechamentos, incluindo o atual, em uma grade uniforme de -1 a 1. O centro do canal é o valor ajustado da observação mais recente. Largura: `StdDevMultiplier * sqrt(sum((close[i] - fitted[i])^2) / Length)`, desvio populacional dos resíduos sem correção por graus de liberdade.

Fechamento estritamente abaixo da banda inferior abre compra; acima da superior abre venda a descoberto. Entrada oposta inverte toda a posição com uma ordem a mercado de `Volume + abs(Position)`; sem posição usa `Volume`. Dentro das bandas mantém a posição. Só negocia com janela completa; sem espera ou stops protetores.

Configuração válida: `0 <= Degree <= 6`, `Length >= Degree + 2`, `StdDevMultiplier` positivo. Grau zero é uma constante. A grade normalizada fixa usa uma base polinomial ortonormal pré-calculada com duas passagens de ortogonalização; cada janela projeta deslocamentos de preço nela com precisão double. C# e Python calculam igualmente; quantidades de ordens permanecem decimal.
