# Estratégia de Seguimento de Tendência KNN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Trend Following KNN é uma estratégia simplificada que mede a variação média de preço em uma janela e compara o preço com uma média móvel.
Compra quando a variação média é positiva e o preço está acima da média móvel, vende quando a variação média é negativa e o preço está abaixo da média móvel.

## Detalhes

- **Critérios de entrada**: variação média positiva/negativa com preço acima/abaixo da média móvel
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `WindowSize` = 20
  - `MaLength` = 50
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
