# Estratégia MACD CCI Lotfy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina MACD e CCI com um fator de escala.
Uma posição é aberta quando ambos os indicadores cruzam limiares extremos na mesma direção.

O valor do MACD é multiplicado por um coeficiente para alinhar a escala com o CCI, permitindo comparação direta com o mesmo limiar.
A abordagem visa capturar reversões de zonas de sobrecompra e sobrevenda.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `CCI < -Threshold` e `MACD * MacdCoefficient < -Threshold`
  - Vendido: `CCI > Threshold` e `MACD * MacdCoefficient > Threshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Um sinal oposto aciona a posição inversa
- **Stops**: Nenhum
- **Valores padrão**:
  - `CciPeriod` = 8
  - `FastPeriod` = 13
  - `SlowPeriod` = 33
  - `MacdCoefficient` = 86000
  - `Threshold` = 85
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: MACD, CCI
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
