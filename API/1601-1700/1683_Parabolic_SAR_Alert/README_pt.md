# Estratégia de Alerta Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora o indicador Parabolic SAR (Stop and Reverse) para detectar possíveis reversões de tendência. Quando o valor do SAR passa de acima do preço para abaixo, o algoritmo interpreta isso como um sinal de alta e abre uma posição comprada. Quando o SAR se move de abaixo do preço para acima, uma posição vendida é aberta.

O fator de aceleração padrão (0.02) e a aceleração máxima (0.2) seguem a configuração clássica do Parabolic SAR. Esses parâmetros controlam a rapidez com que o indicador se aproxima do preço: valores mais altos fazem o SAR reagir mais rápido, mas podem levar a sinais falsos. A estratégia processa apenas velas concluídas e armazena os valores anteriores de SAR e preço para identificar cruzamentos sem consultar dados históricos.

O gerenciamento de risco não está definido explicitamente; o exemplo depende de sinais opostos para sair. Proteção adicional pode ser habilitada através dos mecanismos integrados do framework.

## Detalhes

- **Critérios de entrada**: Parabolic SAR cruza o preço de fechamento.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não definidos.
- **Valores padrão**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
