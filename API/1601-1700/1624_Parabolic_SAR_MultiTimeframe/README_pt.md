# Parabolic SAR Multitemporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Parabolic SAR Multitemporal usa quatro indicadores Parabolic SAR diferentes de períodos superiores
para confirmar uma tendência antes de entrar em uma operação. A estratégia processa velas de 15 minutos e verifica o
estado do SAR em gráficos de 30 minutos, 1 hora e 4 horas. Uma posição comprada só é aberta quando o preço está
acima de todos os valores SAR; uma posição vendida é aberta quando o preço está abaixo de todos os SARs.

O método tenta filtrar o ruído exigindo alinhamento em múltiplos períodos. A posição
é fechada quando a condição oposta aparece.

## Detalhes

- **Critérios de entrada**: Preço relativo ao Parabolic SAR nos períodos 15m/30m/1h/4h.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto de todos os indicadores SAR.
- **Stops**: Usa `StartProtection` para proteção básica, sem valores de stop explícitos.
- **Valores padrão**:
  - `Step15` = 0.062
  - `Step30` = 0.058
  - `Step60` = 0.058
  - `Step240` = 0.058
  - `MaxStep` = 0.1
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário (base 15m com confirmações superiores)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Uso

1. Anexe a estratégia a um instrumento.
2. Ajuste os parâmetros de passo do SAR se necessário.
3. Inicie a estratégia; ela se inscreverá automaticamente em velas de 15m, 30m, 1h e 4h.
