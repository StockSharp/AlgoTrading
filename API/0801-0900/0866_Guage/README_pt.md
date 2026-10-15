# Estratégia Gauge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia imita a biblioteca Gauge do TradingView medindo a posição do preço entre um mínimo e um máximo definidos pelo usuário. Quando o percentual cruza os limiares superior ou inferior, entram operações na direção correspondente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: razão do gauge acima do limiar superior.
  - **Vendido**: razão do gauge abaixo do limiar inferior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Um sinal oposto gera uma saída.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Min value = 0, Max value = 100.
  - Upper threshold = 75%, Lower threshold = 25%.
- **Filtros**:
  - Categoria: Intervalo / Utilitário
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
