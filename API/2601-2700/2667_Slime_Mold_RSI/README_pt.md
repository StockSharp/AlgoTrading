# Estratégia Slime Mold RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma conversão direta do assessor especialista MQL4 "Slime_Mold_RSI_v1.1". A estratégia constrói um único perceptron combinando quatro leituras de RSI (12, 36, 108 e 324) calculadas sobre o preço mediano. Cada valor de RSI é normalizado do intervalo original 0–100 para -1…+1 e multiplicado por um peso configurável. Um cruzamento por zero da soma ponderada inverte a posição.

## Como Funciona
- Calcular o preço mediano de cada vela finalizada e alimentá-lo em quatro indicadores de Índice de Força Relativa com comprimentos de 12, 36, 108 e 324.
- Normalizar cada valor de RSI para o intervalo -1…+1 e aplicar o peso correspondente. Os valores padrão (-100) reproduzem os coeficientes originais do perceptron (`x - 100`).
- Somar as quatro entradas ponderadas para produzir a saída do perceptron da vela atual.
- Comparar o valor mais recente com a saída do perceptron da vela anterior para detectar cruzamentos por zero e gerar sinais de trading.

## Regras de Trading
- **Entrada comprada**: O valor anterior do perceptron está abaixo de zero e o valor atual sobe acima de zero. A estratégia fecha qualquer exposição vendida e estabelece uma posição comprada de tamanho `Volume`.
- **Entrada vendida**: O valor anterior do perceptron está acima de zero e o valor atual cai abaixo de zero. A estratégia sai de qualquer posição comprada e abre uma posição vendida de tamanho `Volume`.
- **Gestão de posições**: Não há alvos de lucro explícitos nem ordens de stop-loss. As posições só são alteradas quando ocorre um novo cruzamento por zero.

## Parâmetros
- `Weight1` – coeficiente aplicado à entrada de RSI normalizada de 12 períodos.
- `Weight2` – coeficiente aplicado à entrada de RSI normalizada de 36 períodos.
- `Weight3` – coeficiente aplicado à entrada de RSI normalizada de 108 períodos.
- `Weight4` – coeficiente aplicado à entrada de RSI normalizada de 324 períodos.
- `CandleType` – período das velas fornecidas à estratégia. O padrão são velas de 1 hora.

## Detalhes
- **Critérios de entrada**: Cruzamento por zero do perceptron RSI ponderado.
- **Comprado/Vendido**: Ambos (sempre no mercado após o primeiro sinal).
- **Critérios de saída**: O cruzamento oposto por zero inverte a posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Weight1` = -100
  - `Weight2` = -100
  - `Weight3` = -100
  - `Weight4` = -100
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoria: Perceptron / Oscilador
  - Direção: Bidirecional
  - Indicadores: RSI (preço mediano)
  - Stops: Não
  - Complexidade: Intermediário (requer quatro indicadores de longo horizonte)
  - Período: Configurável (padrão intradiário por hora)
  - Sazonalidade: Não
  - Redes neurais: Perceptron linear
  - Divergência: Não
  - Nível de risco: Depende do volume e pesos escolhidos

## Notas
- A implementação mantém o registro da saída prévia do perceptron mesmo quando o trading está desabilitado para garantir a continuidade do estado assim que o trading for retomado.
- O preço mediano é usado para corresponder à configuração `PRICE_MEDIAN` do script original do MetaTrader.
- A estratégia inverte posições instantaneamente, portanto considere o potencial deslizamento ao escolher pesos e volume.
