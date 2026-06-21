# Estratégia Rawstocks de Modelo de 15 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Rawstocks 15 Minute Model usa blocos de ordens de swing e níveis de retração de Fibonacci para negociar dentro de uma sessão diária.

## Como funciona
- Detecta máximas e mínimas de swing com um filtro ATR.
- Constrói blocos de ordens de alta e baixa e calcula os níveis Fibonacci de 61,8% e 79%.
- Entra comprado quando o preço toca um bloco de ordens de alta e fecha acima de um nível Fibonacci antes do horário de corte de entrada.
- Entra vendido quando o preço testa um bloco de ordens de baixa e fecha abaixo de um nível Fibonacci.
- Fecha todas as posições às 16:30 ET.

## Parâmetros
- Start Hour
- Start Minute
- Last Entry Hour
- Last Entry Minute
- Force Close Hour
- Force Close Minute
- Fib Level (%)
- Min Swing Size (%)
- Risk/Reward

### Indicadores
- Average True Range
