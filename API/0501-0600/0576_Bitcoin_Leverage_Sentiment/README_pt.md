# Estratégia Bitcoin Sentimento de Alavancagem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia analisa o Z-Score da relação entre posições compradas e vendidas em Bitcoin. Uma operação comprada é aberta quando o Z-Score cruza acima de um limiar configurável e fechada quando cruza abaixo do nível de saída comprado. Operações vendidas usam limiares espelhados. A direção de negociação pode ser limitada a comprado, vendido ou ambos os lados.

## Detalhes

- **Critérios de entrada**:
  - Z-Score cruza acima do limiar de entrada comprado → comprado.
  - Z-Score cruza abaixo do limiar de entrada vendido → vendido.
- **Comprado/Vendido**: Configurável
- **Critérios de saída**:
  - Z-Score cruza abaixo do limiar de saída comprado.
  - Z-Score cruza acima do limiar de saída vendido.
- **Stops**: Nenhum
- **Valores padrão**:
  - Comprimento Z-Score = 252
  - Entrada comprado = 1.0
  - Saída comprado = -1.618
  - Entrada vendido = -1.618
  - Saída vendido = 1.0
  - Tipo de vela = 1 dia
- **Filtros**:
  - Categoria: Sentiment
  - Direção: Ambos
  - Indicadores: SMA, StdDev
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
